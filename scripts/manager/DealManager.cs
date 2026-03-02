using Godot;
using System;
using System.Collections.Generic;
using Euler.Global;

public partial class DealManager : Node
{
    public GameCore GameCore { get; private set; }      // 只有服务器有
    private TableManager tableManager;

    private List<CardData> localHands;

    private Queue<int> dealQueue;
    private Timer timer;

    public static DealManager Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance == null)
            Instance = this;
        else
            QueueFree();
    }

    /// <summary>
    /// 初始化 DealManager
    /// 所有客户端都必须调用
    /// 服务器传入 GameCore，客户端传 null
    /// </summary>
    public void Init(GameCore _gameCore, TableManager _tableManager)
    {
        GameCore = _gameCore;      // 服务器才有
        tableManager = _tableManager;

        dealQueue = new Queue<int>();
        timer = new Timer
        {
            WaitTime = 0.3f,
            OneShot = false,
            Autostart = false
        };
        AddChild(timer);
        timer.Timeout += OnDealTimeout;
    }

    /// <summary>
    /// 服务器开始发牌
    /// </summary>
    public void StartDeal()
    {
        if (!Multiplayer.IsServer() || GameCore == null)
            return; // 只有服务器执行发牌

        dealQueue.Clear();
        for (int i = 0; i < GameSettings.PLAYER_COUNT * GameSettings.CARD_PRE_PLAYER; i++)
            dealQueue.Enqueue(i % GameSettings.PLAYER_COUNT);

        timer.Start();
    }

    private void OnDealTimeout()
    {
        if (dealQueue.Count == 0)
        {
            timer.Stop();
            GD.Print("发牌结束");
            return;
        }

        int logicalSeat = dealQueue.Dequeue();
        GameCore.DrawCardForPlayer(logicalSeat);
        List<CardData> cardDatas = GameCore.GetPlayerHand(logicalSeat);
        int[] ids = CardData.Serialize(cardDatas); // 只传一张

        long peerId = NetworkManager.Instance.GetPeerIdBySeat(logicalSeat);

        if (peerId == -1)
        {
            // 玩家不存在，跳过
        }
        else if (peerId == Multiplayer.GetUniqueId())
        {
            // 自己发给自己
            ReceiveHand(logicalSeat, ids);
        }
        else
        {
            NetworkManager.Instance.SendHand(peerId, logicalSeat, ids);
        }
    }

    /// <summary>
    /// 所有客户端调用显示牌
    /// </summary>
    public void ReceiveHand(int logicalSeat, int[] ids)
    {
        if (tableManager == null)
        {
            GD.PrintErr("DealManager: tableManager 未初始化！");
            return;
        }
        localHands = CardData.Deserialize(ids);
        int viewSeat = NetworkManager.Instance.GetViewSeat(logicalSeat);
        tableManager.ShowPlayerHand(viewSeat, localHands);
    }
}