namespace MyProject.Function
{
    public class B2cCustomAttributeHelper
    {
        internal readonly string _b2cExtensionAppClientId;

        internal B2cCustomAttributeHelper(string b2cExtensionAppClientId)
        {
            _b2cExtensionAppClientId = b2cExtensionAppClientId.Replace("-", "");
        }

        internal string GetCompleteAttributeName(string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new System.ArgumentException("Parameter cannot be null", nameof(attributeName));
            }

            return $"extension_{_b2cExtensionAppClientId}_{attributeName}";
        }
    }
    public class ResponseContent
    {
        private const string APIVERSION = "1.0.0";

        public ResponseContent()
        {
            this.Version = ResponseContent.APIVERSION;
            this.Action = "Continue";
        }

        public ResponseContent(string action = "Continue", string userMessage = "", string status = "200")
        {
            this.Version = ResponseContent.APIVERSION;
            this.Action = action;
            this.UserMessage = userMessage;
            this.Status = status;
        }

        public string Version { get; }
        public string Action { get; set; }
        public string UserMessage { get; set; }
        public string Status { get; set; }

    }
}