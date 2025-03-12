namespace WebApiWithDocker.Data
{
    public class DocumentEncriptHTTPBody
    {
        public required string privatekeybase64 { get; set; }
        public required string textbase64 { get; set; }
    }

    public class DecryptionResponse
    {
        public string? decryptedText { get; set; }
    }
    public class FlowResultClass
    {
        public required string holdingIdentityShortHash { get; set; }
        public string clientRequestId { get; set; }
        public string flowId { get; set; }
        public string flowStatus { get; set; }
        public string flowResult { get; set; }
        public FlowResultErrorClass flowError { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class FlowResultErrorClass
    {
        public string type { get; set; }
        public string message { get; set; }
    }

    public class VirtualNode
    {
        public string HoldingIdentity { get; set; }
        public string CpiIdentifier { get; set; }
        public string VaultDdlConnectionId { get; set; }
        public string VaultDmlConnectionId { get; set; }
        public string CryptoDdlConnectionId { get; set; }
        public string CryptoDmlConnectionId { get; set; }
        public string UniquenessDdlConnectionId { get; set; }
        public string UniquenessDmlConnectionId { get; set; }
        public string HsmConnectionId { get; set; }
        public string FlowP2pOperationalStatus { get; set; }
        public string FlowStartOperationalStatus { get; set; }
        public string FlowOperationalStatus { get; set; }
        public string VaultDbOperationalStatus { get; set; }
        public bool OperationInProgress { get; set; }
        public object ExternalMessagingRouteConfiguration { get; set; }
    }
}
