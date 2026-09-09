using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// Generates CHED IDs in the TRACES format, <c>CHED{type}.{country}.{year}.{serial}</c>.
/// </summary>
/// <remarks>
/// Sequential rather than random: a test that creates two CHEDs should get two IDs a human can tell
/// apart, and a repeated run should not depend on luck to avoid a collision.
/// </remarks>
public sealed class ChedIds(string countryCode = "XI")
{
    private int _serial;

    public string Next(ChedType type) =>
        $"CHED{type}.{countryCode}.{DateTime.UtcNow.Year}.{Interlocked.Increment(ref _serial):D7}";
}
