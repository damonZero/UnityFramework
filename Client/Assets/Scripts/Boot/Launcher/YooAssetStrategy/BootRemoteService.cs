using System.Collections.Generic;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// AOT-side remote URL provider for YooAsset host-play-mode downloads.
    /// Tests / external callers may override URL resolution via
    /// <see cref="CustomUrlProvider"/>.
    /// </summary>
    public sealed class BootRemoteService : IRemoteService
    {
        public static System.Func<string, IReadOnlyList<string>> CustomUrlProvider;

        private readonly string _baseUrl;

        public BootRemoteService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            if (CustomUrlProvider != null)
                return CustomUrlProvider.Invoke(fileName);

            return new List<string> { $"{_baseUrl}/{fileName}" };
        }
    }
}
