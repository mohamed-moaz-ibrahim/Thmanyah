using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Thmanyah.Shared.Domain;
using Thmanyah.Shared.Configurations;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// Factory for creating and managing episode storage strategies.
    /// Implements factory pattern for polymorphic strategy instantiation.
    /// </summary>
    public class EpisodeStorageFactory
    {
        private readonly Dictionary<EpisodeStorageType, Func<IEpisodeStorageStrategy>> _strategies;
        private readonly IOptions<StorageConfiguration> _configuration;

        public EpisodeStorageFactory(IOptions<StorageConfiguration> configuration)
        {
            _configuration = configuration;
            _strategies = new Dictionary<EpisodeStorageType, Func<IEpisodeStorageStrategy>>
            {
                { EpisodeStorageType.LocalFile, () => new LocalStorageStrategy(_configuration) },
                { EpisodeStorageType.AzureStorage, () => new AzureStorageStrategy(_configuration) },
                { EpisodeStorageType.AWSStorage, () => new AWSStorageStrategy(_configuration) },
                { EpisodeStorageType.ExternalUrl, () => new ExternalUrlStrategy() }
            };
        }

        /// <summary>
        /// Register a custom storage strategy.
        /// </summary>
        public void RegisterStrategy(EpisodeStorageType type, Func<IEpisodeStorageStrategy> factory)
        {
            _strategies[type] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Get or create a storage strategy by type.
        /// </summary>
        public IEpisodeStorageStrategy GetStrategy(EpisodeStorageType strategyType)
        {
            if (!_strategies.TryGetValue(strategyType, out var factory))
                throw new KeyNotFoundException($"Storage strategy '{strategyType}' not registered. Available: {string.Join(", ", _strategies.Keys)}");

            return factory();
        }

        /// <summary>
        /// Get strategy by string name (for backward compatibility).
        /// </summary>
        public IEpisodeStorageStrategy GetStrategy(string strategyName)
        {
            if (!Enum.TryParse<EpisodeStorageType>(strategyName, ignoreCase: true, out var strategyType))
            {
                throw new ArgumentException($"Unknown storage strategy '{strategyName}'", nameof(strategyName));
            }

            return GetStrategy(strategyType);
        }

        /// <summary>
        /// Get the default strategy (local file storage).
        /// </summary>
        public IEpisodeStorageStrategy GetDefaultStrategy()
        {
            return GetStrategy(EpisodeStorageType.LocalFile);
        }

        /// <summary>
        /// Get all available strategy types.
        /// </summary>
        public IEnumerable<EpisodeStorageType> GetAvailableStrategies()
        {
            return _strategies.Keys;
        }
    }
}

