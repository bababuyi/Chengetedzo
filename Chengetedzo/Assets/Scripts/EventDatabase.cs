using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EventDatabase", menuName = "Chengetedzo/Event Database")]
public class EventDatabase : ScriptableObject
{
    public List<EventData> events = new List<EventData>();
}