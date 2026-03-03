using System.Collections.Generic;

public class DealResult(CardData _card, List<CardData> _hand)
{
    public CardData CurrentCard { get; set; } = _card;
    public List<CardData> FullHand { get; set; } = _hand;
}