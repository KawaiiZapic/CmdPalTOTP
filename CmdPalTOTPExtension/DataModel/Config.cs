using System.Collections.Generic;

namespace CmdPalTOTPExtension.DataModel {

    public class Authenticator {
        public string Name { get; set; } = "";
        public string Key { get; set; } = "";
        public bool IsEncrypted { get; set; }
    }

    public class AuthenticatorsList {
        public int Version { get; set; }
        public List<Authenticator> Authenticators { get; set; } = [];
    }
}
