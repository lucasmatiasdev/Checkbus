using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Notifications.Queries
{
    public class GetTripNotificationsQueryHandler
        : IRequestHandler<GetTripNotificationsQuery, IReadOnlyList<NotificationDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTripNotificationsQueryHandler(
            ITripRepository tripRepository,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<NotificationDto>> Handle(
            GetTripNotificationsQuery request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new TripNotFoundException();
            }

            var notifications = await _notificationRepository.GetRelevantToTripAsync(
                trip.Id, trip.EventId, cancellationToken);

            // Small per-request cache, same convention as GetPublicReservationsConfirmationQuery
            // Handler's driver lookup — a batch of notifications commonly shares the same author.
            var namesByUserId = new Dictionary<Guid, string?>();
            var dtos = new List<NotificationDto>(notifications.Count);
            foreach (var notification in notifications)
            {
                if (!namesByUserId.TryGetValue(notification.CreatedByUserId, out var createdByName))
                {
                    var user = await _userRepository.FindByIdAsync(notification.CreatedByUserId, cancellationToken);
                    createdByName = user is null ? null : $"{user.Name} {user.Surname}";
                    namesByUserId[notification.CreatedByUserId] = createdByName;
                }

                dtos.Add(new NotificationDto
                {
                    Id = notification.Id,
                    TargetType = notification.TargetType,
                    TargetId = notification.TargetId,
                    Reason = notification.Reason,
                    Message = notification.Message,
                    CreatedAt = notification.CreatedAt,
                    CreatedByName = createdByName
                });
            }

            return dtos;
        }
    }
}
