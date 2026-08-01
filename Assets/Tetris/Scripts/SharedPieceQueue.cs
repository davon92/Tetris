using System;

public sealed class SharedPieceQueue
{
    private readonly SevenBagRandomizer randomizer;
    private readonly float closeClaimWindow;

    private object lastClaimant;
    private object mirroredClaimant;
    private TetriminoType lastClaimedType;
    private float lastClaimTime = float.NegativeInfinity;
    private TetriminoType nextType;

    public SharedPieceQueue(int seed, float closeClaimWindow)
    {
        randomizer = new SevenBagRandomizer(seed);
        this.closeClaimWindow = Math.Max(0f, closeClaimWindow);
        nextType = randomizer.Next();
    }

    public TetriminoType NextType => nextType;
    public float CloseClaimWindow => closeClaimWindow;

    /// <summary>
    /// The contested upcoming pieces, nearest first. Both players read the same
    /// queue — fighting over these is the point of the shared bag.
    /// </summary>
    public TetriminoType[] PeekUpcoming(int count)
    {
        if (count <= 0)
            return Array.Empty<TetriminoType>();

        TetriminoType[] upcoming = new TetriminoType[count];
        upcoming[0] = nextType;
        if (count > 1)
        {
            TetriminoType[] fromBag = randomizer.Peek(count - 1);
            Array.Copy(fromBag, 0, upcoming, 1, count - 1);
        }

        return upcoming;
    }

    public TetriminoType Claim(object claimant, float claimTime)
    {
        if (claimant == null)
            throw new ArgumentNullException(nameof(claimant));

        bool differentPlayer = !ReferenceEquals(lastClaimant, claimant);
        bool hasNotMirroredClaim = !ReferenceEquals(mirroredClaimant, claimant);
        float timeSinceLastClaim = claimTime - lastClaimTime;
        bool isCloseClaim =
            lastClaimant != null &&
            differentPlayer &&
            hasNotMirroredClaim &&
            timeSinceLastClaim >= 0f &&
            timeSinceLastClaim <= closeClaimWindow;

        if (isCloseClaim)
        {
            mirroredClaimant = claimant;
            return lastClaimedType;
        }

        TetriminoType claimedType = nextType;
        lastClaimedType = claimedType;
        lastClaimant = claimant;
        mirroredClaimant = null;
        lastClaimTime = claimTime;
        nextType = randomizer.Next();
        return claimedType;
    }
}
