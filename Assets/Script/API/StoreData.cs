using UnityEngine;

public static class StoreData
{
    // Keys from AuthManager
    public const string tokenKey = "AUTH_TOKEN";
    public const string orgKey = "AUTH_ORG";
    public const string passwordKey = "AUTH_PASSWORD";
    
    // Test2Dproject doesn't use these but APIUseCase might reference them
    public static bool isDev = false; 

    public static string GetToken()
    {
        return PlayerPrefs.GetString(tokenKey, "");
    }

    public static string GetOrgId()
    {
        string org = PlayerPrefs.GetString(orgKey, "");
        if (string.IsNullOrEmpty(org)) return "limbic"; // Default fallback
        return org;
    }

    public static string GetPassword()
    {
        string pw = PlayerPrefs.GetString(passwordKey, "");
        // Default password for testing (เหมือน Bangpla ที่ใช้ User@1234)
        if (string.IsNullOrEmpty(pw)) return "User@1234";
        return pw;
    }

    public static void SetOrganizeId(bool status, string org)
    {
        PlayerPrefs.SetString(orgKey, org);
        PlayerPrefs.Save();
    }
    
    // Fallback for User object if needed
    public static void SetUser(User u)
    {
        // No-op for now in Test2D
    }
}
