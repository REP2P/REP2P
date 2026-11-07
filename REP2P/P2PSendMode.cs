namespace REP2P
{
    /// <summary>
    /// Message Reliablity
    /// </summary>
    public enum P2PSendMode
    {
        /// <summary>
        /// slower but guarantees the message being sent
        /// </summary>
        Reliable,
        /// <summary>
        /// faster but no guarantees
        /// </summary>
        Unreliable,
    }
}
