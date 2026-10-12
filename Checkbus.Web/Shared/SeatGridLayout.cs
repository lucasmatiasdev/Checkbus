namespace Checkbus.Web.Shared;

/// <summary>
/// Shared 2+2-with-aisle seat grid layout math (odd/tasks/mejoras-detalle-viaje.md Fase E2).
/// Extracted from <c>TripReservation.razor</c>'s private <c>BuildSeatRows</c> so both the
/// public, interactive reservation grid and the Planificador's read-only seat-map panel on
/// <c>TripDetail.razor</c> reuse the exact same row/aisle math instead of duplicating it.
/// </summary>
public static class SeatGridLayout
{
    /// <summary>
    /// Builds rows of 5 slots each — <c>[seatA, seatB, aisle-gap, seatC, seatD]</c> — for the
    /// given <paramref name="capacity"/>. A <c>null</c> slot marks the aisle gap between the
    /// left and right seat pairs; every non-null slot is a 1-based seat number.
    /// </summary>
    public static List<int?[]> BuildSeatRows(int capacity)
    {
        var rows = new List<int?[]>();
        var seatNumber = 1;

        while (seatNumber <= capacity)
        {
            var row = new int?[5];
            for (var slot = 0; slot < 5 && seatNumber <= capacity; slot++)
            {
                row[slot] = slot == 2 ? null : seatNumber++;
            }
            rows.Add(row);
        }

        return rows;
    }
}
