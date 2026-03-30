using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Abel.TranHuongDao.Core
{
    public class TDHandService
    {
        public readonly struct CardAddedEvent
        {
            public readonly string TowerID;
            public readonly int Tier;
            public readonly Vector2 StartScreenPos;

            public CardAddedEvent(string towerID, int tier, Vector2 startScreenPos)
            {
                TowerID = towerID;
                Tier = tier;
                StartScreenPos = startScreenPos;
            }
        }

        private readonly List<string> _currentCards = new List<string>();
        private readonly FD.IEventBus _eventBus;

        [Inject]
        public TDHandService(FD.IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void AddCard(string towerID, int tier, Vector2 startScreenPos)
        {
            _currentCards.Add(towerID);
            
            _eventBus?.Publish(new CardAddedEvent(towerID, tier, startScreenPos));

            Debug.Log($"[TDHandService] Card added via EventBus: {towerID} (Tier {tier})");
        }

        public IReadOnlyList<string> CurrentCards => _currentCards;
    }
}
