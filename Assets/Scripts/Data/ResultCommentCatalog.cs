using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResultCommentCatalog", menuName = "ScriptableObject/ResultCommentCatalog")]
public class ResultCommentCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public int startYear;
        public int endYear;
        [TextArea(1, 3)] public List<string> comments = new List<string>();
        [TextArea(1, 3)] public List<string> commentsEnglish = new List<string>();
    }

    public List<Entry> entries = new List<Entry>();

    public string GetComment(float score)
    {
        int year = Mathf.FloorToInt(score);

        if (year >= 10000)
        {
            return  Application.systemLanguage == SystemLanguage.Japanese
                ? "この美しい海からゴミが消えます様に"
                : "May the trash disappear from this beautiful ocean.";
        }

        if (year >= 5000)
        {
            return Application.systemLanguage == SystemLanguage.Japanese
                ? "どこまで頑張るつもりなの？凄すぎる"
                : "How far are you planning to go? That's incredible.";
        }

        if (year >= 3000)
        {
            return Application.systemLanguage == SystemLanguage.Japanese
                ? "信じられない…3,000年を超えてるよ？"
                : "I can't believe it... You've passed 3,000 years!";
        }

        Entry entry = entries.Find(item => year >= item.startYear && year <= item.endYear);
       
        if (entry != null)
        {
            List<string> commentsToUse =
                 Application.systemLanguage == SystemLanguage.Japanese
                     ? entry.comments
                     : entry.commentsEnglish;

            if (commentsToUse.Count > 0)
            {
                return commentsToUse[UnityEngine.Random.Range(0, commentsToUse.Count)];
            }
        }

        return Application.systemLanguage == SystemLanguage.Japanese
            ? "海の姿が少しずつ変わり始めている。"
            : "The ocean is slowly beginning to change.";
    }
}
