using System.Text.RegularExpressions;
using UnityEngine;

public static class SocketUtility
{
    public const string WILDCARD = "-1";
    public const int WILDCARD_VALUE = -1;

    /// <summary>
    /// Tries to find the first number in the socket name which should be its ID
    /// then parses it to string
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    public static bool ParseSocketIndex (string socket, out int id)
    {
        string num = Regex.Match(socket, @"^-?\d+").Value;

        if (!string.IsNullOrEmpty(num)) {
            id = int.Parse(num);
            return true;
        } else {
            id = WILDCARD_VALUE;
            return false;
        }
    }

    public static bool ParseSocketLastDigit (string socket, out int id)
    {
        string num = Regex.Match(socket, @"\d$").Value;

        if (!string.IsNullOrEmpty(num)) {
            id = int.Parse(num);
            return true;
        } else {
            id = WILDCARD_VALUE;
            return false;
        }
    }

    public static bool IsWildCard (string socket) => socket == WILDCARD;

    public static bool IsInvalid (string socket) => !IsValid(socket);
    public static bool IsValid (string socket)
    {
        return IsWildCard(socket) || Regex.IsMatch(socket, @"^\d+([fs]?$)|(_\d$)");
    }

    public static bool IsDirectional (string socket)
    {
        return Regex.IsMatch(socket, @"^\d+_\d$");
    }

    public static bool IsSymmetric (string socket)
    {
        return Regex.IsMatch(socket, @"^\d+s$");
    }

    public static bool IsFlippable (string socket)
    {
        return Regex.IsMatch(socket, @"^\d+(f?)$");
    }

    public static bool IsFlipped (string socket)
    {
        return Regex.IsMatch(socket, @"^\d+f$");
    }



    public static bool Matching (string socket1, string socket2, bool isVertical)
    {
        // Not matching if invalid sockets tag or mismatching IDs
        if (IsInvalid(socket1) || IsInvalid(socket2))
            return false;
        if (!(ParseSocketIndex(socket1, out int id1) && ParseSocketIndex(socket2, out int id2) && id1 == id2))
            return false;

        if (IsWildCard(socket1) || IsWildCard(socket2))
            return false;

        if (isVertical) {
            return socket1 == socket2;
        } else {
            if (IsDirectional(socket1) && IsDirectional(socket2)) {
                Debug.LogWarning("Directional Socket found as a side socket. It isn't supposed to happen and is likely an issue.");
                return socket1 == socket2;
            }

            if (IsSymmetric(socket1) && IsSymmetric(socket2))
                return socket1 == socket2;

            if (IsFlippable(socket1) && IsFlippable(socket2)) {
                return IsFlipped(socket1) != IsFlipped(socket2);
            }
        }

        return false;
    }
}