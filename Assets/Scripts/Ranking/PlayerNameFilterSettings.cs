using UnityEngine;

[CreateAssetMenu(fileName = "PlayerNameFilter", menuName = "DriftOcean/Player Name Filter")]
public class PlayerNameFilterSettings : ScriptableObject
{
    [Tooltip("NGワードを追加・変更できます。大文字小文字、全角英字、文字間の空白等は同一視します。")]
    public string[] blockedWords =
    {
        "死ね", "nigger", "chink", "spic", "kike", "gook", "faggot", "raghead",
        "retard", "tranny", "cunt", "cock", "pussy", "porn", "rape"
    };

    [Tooltip("有効にすると英単語の一部に含まれる場合も禁止します。無効なら英字の単語境界で判定します。")]
    public bool matchInsideEnglishWords = true;
}
