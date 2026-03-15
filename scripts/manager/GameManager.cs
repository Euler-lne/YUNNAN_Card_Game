using Euler.Event;
using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    private UIManager uiManager;
    private Player player;
    private DealManager dealManager;
    private TurnManager turnManager;
    private GameCore gameCore = null;

    public override void _Ready()
    {
        uiManager = GetNode<UIManager>("../CanvasLayer/UIManager");
        dealManager = GetNode<DealManager>("../DealManager");
        player = GetNode<Player>("../Player");
        // 所有客户端都初始化 DealManager
        gameCore = Multiplayer.IsServer() ? new GameCore() : null;
        dealManager.Init(gameCore);

        if (Multiplayer.IsServer())
        {
            NetworkManager.Instance.AssignServerSeat();
            uiManager.UpdatePlayerCount(NetworkManager.Instance.TotalPlayers);
            uiManager.ConnectStartButtonPressed(StartGame);
            turnManager = new();
            turnManager.Init(GetNode<TurnRequest>("../TurnRequest"), gameCore.GetDealerSeat(), gameCore);
            NetworkManager.Instance.OnTotalPlayersChanged += uiManager.UpdatePlayerCount;
            dealManager.DealEndEvent += OnDealEndEvent;
            turnManager.TurnOver += OnTurnOver;
        }
    }
    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            dealManager.DealEndEvent -= OnDealEndEvent;
            NetworkManager.Instance.OnTotalPlayersChanged -= uiManager.UpdatePlayerCount;
            turnManager.TurnOver -= OnTurnOver;
        }
    }

    private void StartGame()
    {
        if (!Multiplayer.IsServer())
            return;
        gameCore.StartGame();
        // 开始发牌
        dealManager.StartDeal();
        // 开始回合
    }

    private void OnDealEndEvent()
    {
        if (!Multiplayer.IsServer())
            return;
        GD.Print("回合开始");
        gameCore.SetCurrentGamePhase(GamePhase.PLAYING);
        CardData trumpCardData = new(gameCore.GetTrumpSuit(), gameCore.GetCurrentRank(gameCore.GetDealerSeat()));
        turnManager.SetFirstTurn();
        Rpc(nameof(RpcSetTrumpCardData), CardData.Serialize(trumpCardData));
        turnManager.StartTurn(gameCore.GetDealerSeat());
    }

    private void OnTurnOver()
    {
        if (!Multiplayer.IsServer())
            return;
        if (gameCore.HaveWinner())
        {
            GD.Print("GameCore得知有赢家了");
        }
        else
        {
            // FIXME:第二轮的叫主失效了
            StartGame();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetTrumpCardData(int id)
    {
        CardData cardData = CardData.Deserialize(id);
        TurnEvent.OnSetTrumpCardDataEvent(cardData);
    }
}