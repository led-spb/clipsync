using System.Configuration;

namespace clipsync;

class AppSettings: ApplicationSettingsBase{

    private String defaultUserName{
        get {
            var nameParts = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split(@"\");
            return nameParts.Length == 1 ? nameParts[0] : nameParts[1];
        }
    }

    private String defaultDomain{
        get {
            var nameParts = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split(@"\");
            return nameParts.Length == 1 ? "" : nameParts[0];
        }
    }

    [UserScopedSetting()]
    public String syncUrl{
        get => (string)this["syncURL"];
        set => this["syncURL"] = value;
    }

    [UserScopedSetting()]
    public String userName{
        get => (string)(this["userName"] != null ? this["userName"] : defaultUserName);
        set => this["userName"] = value;
    }

    [UserScopedSetting()]
    public String domain{
        get => (string)(this["domain"] != null ? this["domain"] : defaultDomain);
        set => this["domain"] = value;
    }

}