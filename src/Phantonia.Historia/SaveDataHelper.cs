namespace Phantonia.Historia;

public static class SaveDataHelper
{
    public static bool ValidateSaveData(byte[] saveData, ulong fingerprint)
    {
        // saveData[0] is the version
        // if we ever increment the version, change this code to support all possible versions
        if (saveData.Length == 0 || saveData[0] != 1)
        {
            return false;
        }

        if (!VerifyChecksum(saveData))
        {
            return false;
        }

        if (!VerifyFingerprint(saveData, fingerprint))
        {
            return false;
        }

        return true;
    }

    private static bool VerifyChecksum(byte[] saveData)
    {
        byte checksum = 0;

        unchecked
        {
            for (int i = 0; i < saveData.Length - 1; i++)
            {
                checksum += saveData[i];
            }
        }

        return saveData[^1] == checksum;
    }

    private static bool VerifyFingerprint(byte[] saveData, ulong fingerprint)
    {
        ulong savedFingerprint =
            saveData[1]
            | ((ulong)saveData[2] << 8)
            | ((ulong)saveData[3] << 16)
            | ((ulong)saveData[4] << 24)
            | ((ulong)saveData[5] << 32)
            | ((ulong)saveData[6] << 40)
            | ((ulong)saveData[7] << 48)
            | ((ulong)saveData[8] << 56);

        return savedFingerprint == fingerprint;
    }
}
