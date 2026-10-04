namespace Checkbus.ApiService.Infrastructure.Implementations.Maps
{
    /// <summary>
    /// Google Maps configuration. Unlike <c>JwtOptions</c>/<c>FileStorageOptions</c>, this is
    /// NOT throw-if-missing: the API key has not been provisioned yet, so binding must tolerate
    /// a missing "GoogleMaps" config section or an empty <see cref="ApiKey"/>. The real Directions
    /// call only fails (at call time, not at startup) once something actually invokes
    /// <c>IDirectionsService</c> with no usable key.
    /// </summary>
    public class GoogleMapsOptions
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
