using System;
namespace Euler.Event
{
    public static class EventBus
    {
        public static Action<int[]> SelectCardEvent;
        public static void OnSelectCardEvent(int[] ids)
        {
            SelectCardEvent?.Invoke(ids);
        }

        public static Action<int, int[], bool, GamePhase> PlayCardEvent;
        public static void OnPlayCardEvent(int playSeat, int[] ids, bool isBack, GamePhase gamePhase)
        {
            PlayCardEvent?.Invoke(playSeat, ids, isBack, gamePhase);
        }
    }
    public static class DealEvent
    {
        public static Action<DeclareOption> SetDeclareEvent;
        public static void OnSetDeclareEvent(DeclareOption declareOption)
        {
            SetDeclareEvent?.Invoke(declareOption);
        }
        public static Action<bool> JudgeDeclareEvent;
        public static void OnJudgeDeclareRequestEvent(bool isValid)
        {
            JudgeDeclareEvent?.Invoke(isValid);
        }
        public static Action<DeclareOption, long> DeclareRequestEvent;
        public static void OnDeclareRequestEvent(DeclareOption option, long peerId)
        {
            DeclareRequestEvent?.Invoke(option, peerId);
        }
        public static Action<bool> JudgeConfirmEvent;
        public static void OnJudgeConfirmRequestEvent(bool isValid)
        {
            JudgeConfirmEvent?.Invoke(isValid);
        }
        public static Action CancelRequestEvent;
        public static void OnCancelRequestEvent()
        {
            CancelRequestEvent?.Invoke();
        }

        public static Action<DeclareOption, long, int[]> ConfirmRequestEvent;
        public static void OnConfirmRequestEvent(DeclareOption option, long peerId, int[] ids)
        {
            ConfirmRequestEvent?.Invoke(option, peerId, ids);
        }

        public static Func<CardData> ConfirmDardTrumpEvent;
        public static CardData OnConfirmDardTrumpEvent()
        {
            return ConfirmDardTrumpEvent?.Invoke();
        }

        public static Action ChooseHoleEvent;
        public static void OnChooseHoleEvent()
        {
            ChooseHoleEvent?.Invoke();
        }

        public static Action<bool> ServerNotifyChooseHoleResultEvent;
        public static void OnServerNotifyChooseHoleResultEvent(bool isBig)
        {
            ServerNotifyChooseHoleResultEvent?.Invoke(isBig);
        }

        public static Action<bool> ClientNotifyChooseHoleResultEvent;
        public static void OnClientNotifyChooseHoleResultEvent(bool isBig)
        {
            ClientNotifyChooseHoleResultEvent?.Invoke(isBig);
        }

    }

    public static class UIEvent
    {
        public static Action<string, int> ChangeNameEvent;
        public static void OnChangeNameEvent(string name, int seat)
        {
            ChangeNameEvent?.Invoke(name, seat);
        }

        public static Action<string, int> ChangeAvatarEvent;
        public static void OnChangeAvatarEvent(string path, int seat)
        {
            ChangeAvatarEvent?.Invoke(path, seat);
        }

        public static Action<bool, int> ChangeTrumpEvent;
        public static void OnChangeTrumpEvent(bool isTrump, int seat)
        {
            ChangeTrumpEvent?.Invoke(isTrump, seat);
        }

        public static Action<int, int> ChangeCardNumEvent;
        public static void OnChangeCardNumEvent(int cardNum, int seat)
        {
            ChangeCardNumEvent?.Invoke(cardNum, seat);
        }

        public static Action<Suit> ChangeTrumpSuitEvent;
        public static void OnChangeTrumpSuitEvent(Suit suit)
        {
            ChangeTrumpSuitEvent?.Invoke(suit);
        }
    }
}