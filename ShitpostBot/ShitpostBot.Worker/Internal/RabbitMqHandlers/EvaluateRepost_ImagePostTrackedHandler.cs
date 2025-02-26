﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;
using NServiceBus;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Worker
{
    internal class EvaluateRepost_ImagePostTrackedHandler : IHandleMessages<ImagePostTracked>
    {
        private readonly ILogger<EvaluateRepost_ImagePostTrackedHandler> logger;
        private readonly IImageFeatureExtractorApi imageFeatureExtractorApi;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IUnitOfWork unitOfWork;

        private readonly IChatClient chatClient;
        private readonly IOptions<RepostServiceOptions> options;

        private readonly string[] repostReactions =
        {
            ":police_car:",
            // ":regional_indicator_r:",
            // ":regional_indicator_e:",
            // ":regional_indicator_p:",
            // ":regional_indicator_o:",
            // ":regional_indicator_s:",
            // ":regional_indicator_t:",
            ":rotating_light:"
        };

        public EvaluateRepost_ImagePostTrackedHandler(ILogger<EvaluateRepost_ImagePostTrackedHandler> logger,
            IImageFeatureExtractorApi imageFeatureExtractorApi, IDateTimeProvider dateTimeProvider, IUnitOfWork unitOfWork,
            IOptions<RepostServiceOptions> options, IChatClient chatClient)
        {
            this.logger = logger;
            this.imageFeatureExtractorApi = imageFeatureExtractorApi;
            this.dateTimeProvider = dateTimeProvider;
            this.unitOfWork = unitOfWork;
            this.options = options;
            this.chatClient = chatClient;
        }

        public async Task Handle(ImagePostTracked message, IMessageHandlerContext context)
        {
            var utcNow = dateTimeProvider.UtcNow;

            var postToBeEvaluated = await unitOfWork.ImagePostsRepository.GetById(message.ImagePostId);
            if (postToBeEvaluated == null)
            {
                // TODO: handle
                throw new NotImplementedException();
            }

            var imageFeatures = await imageFeatureExtractorApi.ExtractImageFeaturesAsync(postToBeEvaluated.ImagePostContent.Image.ImageUri.ToString());
            postToBeEvaluated.ImagePostContent.Image =
                postToBeEvaluated.ImagePostContent.Image with { ImageFeatures = new ImageFeatures(imageFeatures.ImageFeatures) };

            var searchedPostHistory = await unitOfWork.ImagePostsRepository.GetHistory(DateTimeOffset.MinValue, postToBeEvaluated.PostedOn);

            var (closestAndOldestExistingPostToNewPost, stopwatch) = BenchmarkedExecute(() =>
                searchedPostHistory
                    .MaxBy(p => p.GetSimilarityTo(postToBeEvaluated))
                    .MinBy(p => p.PostedOn)
                    .FirstOrDefault()
            );

            var tookMillis = stopwatch.ElapsedMilliseconds;

            logger.LogDebug($"Found closest post in {tookMillis}ms out of {searchedPostHistory.Count} possible posts");

            if (closestAndOldestExistingPostToNewPost == null)
            {
                // no post from a different user within the repost search period. new post gets a free pass
                logger.LogDebug($"ImagePost {postToBeEvaluated.Id} marked as not a repost.");
                
                var statistics = new PostStatistics(null);
                
                postToBeEvaluated.SetPostStatistics(statistics);
            }
            else
            {
                var similarity = postToBeEvaluated.GetSimilarityTo(closestAndOldestExistingPostToNewPost);
                var mostSimilarTo = new PostStatisticsMostSimilarTo(closestAndOldestExistingPostToNewPost.Id, (decimal)similarity);
                var statistics = new PostStatistics(mostSimilarTo);

                postToBeEvaluated.SetPostStatistics(statistics);

                logger.LogInformation(
                    $"ImagePost {postToBeEvaluated.Id} has a similarity of {mostSimilarTo.Similarity} with ImagePost {mostSimilarTo.SimilarToPostId}. It took {tookMillis}ms");
            }

            await unitOfWork.SaveChangesAsync();

            // TODO: move to a different handler
            if (postToBeEvaluated.Statistics?.MostSimilarTo != null &&
                postToBeEvaluated.Statistics.MostSimilarTo.Similarity >= options.Value.RepostSimilarityThreshold)
            {
                var identification = new MessageIdentification(
                    postToBeEvaluated.ChatGuildId,
                    postToBeEvaluated.ChatChannelId,
                    postToBeEvaluated.PosterId,
                    postToBeEvaluated.ChatMessageId
                );

                foreach (var repostReaction in repostReactions)
                {
                    await chatClient.React(identification, repostReaction);
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }
        }

        private (TResult, Stopwatch) BenchmarkedExecute<TResult>(Func<TResult> func)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = func();

            stopwatch.Stop();

            return (result, stopwatch);
        }
    }
}