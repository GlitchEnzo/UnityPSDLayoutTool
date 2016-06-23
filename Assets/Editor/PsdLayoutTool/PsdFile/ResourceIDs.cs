namespace PhotoshopFile
{
    /// <summary>
    /// The possible resource IDs for layer data.
    /// </summary>
    public enum ResourceIDs
    {
        /// <summary>
        /// Resolution resource data.
        /// </summary>
        ResolutionInfo = 1005,

        /// <summary>
        /// Alpha channel name resource data.
        /// </summary>
        AlphaChannelNames = 1006,

        /// <summary>
        /// XML resource data.
        /// </summary>
        XMLInfo = 1060,

        /// <summary>
        /// (Photoshop CC) Path Selection State. 4 bytes (descriptor version = 16), 
        /// Descriptor (see See Descriptor structure) Information about the current path selection state.
        /// </summary>
        PsCCPathSelectionState = 1088,

        /// <summary>
        /// (Photoshop CC) Origin Path Info. 4 bytes (descriptor version = 16), 
        /// Descriptor (see See Descriptor structure) Information about the origin path data.
        /// </summary>
        PsCCOrignPathInfo = 3000,
    }
}
