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
    }

    public List<Entry> entries = new List<Entry>();

    public string GetComment(float score)
    {
        int year = Mathf.FloorToInt(score);

        if (year >= 10000)
        {
            return "この美しい海からゴミが消えます様に";
        }

        if (year >= 5000)
        {
            return "どこまで頑張るつもりなの？凄すぎる";
        }

        if (year >= 3000)
        {
            return "信じられない…3,000年を超えてるよ？";
        }

        Entry entry = entries.Find(item => year >= item.startYear && year <= item.endYear);
        if (entry != null && entry.comments.Count > 0)
        {
            return entry.comments[UnityEngine.Random.Range(0, entry.comments.Count)];
        }

        return "海の姿が少しずつ変わり始めている。";
    }
}
