using System.Collections.Generic;
using UnityEngine;

public static class CardPlayerProfile
{
    private const string InitializedKey = "CardProfile.Initialized"; /* 초기 지급 완료 여부 */
    private const string GoldKey = "CardProfile.Gold"; /* 성장에 사용하는 보유 골드 */
    private const string WinsKey = "CardProfile.Wins"; /* 내부 기록용 승리 수 */
    private const string LossesKey = "CardProfile.Losses"; /* 내부 기록용 패배 수 */
    private const string DeckKeyPrefix = "CardProfile.Deck."; /* 덱 슬롯 저장 키 접두어 */
    private const string OwnedKeyPrefix = "CardProfile.Owned."; /* 카드 보유 저장 키 접두어 */
    private const string LevelKeyPrefix = "CardProfile.Level."; /* 카드 레벨 저장 키 접두어 */
    private const string ExpKeyPrefix = "CardProfile.Exp."; /* 이전 버전 호환용 경험치 저장 키 접두어 */

    public const int WinGoldReward = 100; /* 승리 시 지급하는 골드 */

    /* 저장 데이터가 없으면 스타터 카드와 기본 덱을 지급합니다. */
    public static void EnsureInitialized()
    {
        if (PlayerPrefs.GetInt(InitializedKey, 0) == 1)
        {
            GrantAllCards();
            return;
        }

        List<string> starterIds = new(); /* 최초 지급 카드 ID 목록 */
        foreach (CardCatalogEntry card in CardCatalog.Cards)
        {
            SetOwned(card.Id, true);
            SetLevel(card.Id, 1);
            SetExp(card.Id, 0);
            starterIds.Add(card.Id);
        }

        for (int i = 0; i < CardCatalog.DeckSize; i++)
            SetDeckCard(i, starterIds[Mathf.Min(i, starterIds.Count - 1)]);

        PlayerPrefs.SetInt(GoldKey, 0);
        PlayerPrefs.SetInt(InitializedKey, 1);
        PlayerPrefs.Save();
    }

    /* 저장된 덱 편성을 전투용 런타임 카드 목록으로 변환합니다. */
    /* 기존 저장 데이터도 과제 프로토타입 기준에 맞춰 모든 카드를 보유 상태로 보정합니다. */
    private static void GrantAllCards()
    {
        bool changed = false; /* 새로 보유 처리한 카드가 있는지 여부 */
        foreach (CardCatalogEntry card in CardCatalog.Cards)
        {
            if (IsOwned(card.Id))
                continue;

            SetOwned(card.Id, true);
            SetLevel(card.Id, 1);
            SetExp(card.Id, 0);
            changed = true;
        }

        if (changed)
            PlayerPrefs.Save();
    }

    public static List<BattleCardRuntime> CreatePlayerDeck(CardOwner owner)
    {
        EnsureInitialized();

        List<BattleCardRuntime> deck = new(); /* 전투에서 사용할 카드 목록 */
        string[] deckIds = GetDeckIds();
        for (int i = 0; i < deckIds.Length; i++)
        {
            CardCatalogEntry entry = CardCatalog.Get(deckIds[i]);
            deck.Add(new BattleCardRuntime(CardCatalog.ToBattleData(entry, GetLevel(entry.Id)), owner));
        }

        return deck;
    }

    /* 전투 결과를 저장하고 승리 시 골드와 카드 획득 보상을 지급합니다. */
    public static string RecordBattleResult(bool isWin)
    {
        EnsureInitialized();

        if (isWin)
        {
            PlayerPrefs.SetInt(WinsKey, GetWins() + 1);
            AddGold(WinGoldReward);
            PlayerPrefs.Save();
            return $"+{WinGoldReward}G";
        }

        PlayerPrefs.SetInt(LossesKey, GetLosses() + 1);
        PlayerPrefs.Save();
        return "No reward";
    }

    /* 현재 저장된 덱 카드 ID 목록을 반환합니다. */
    public static string[] GetDeckIds()
    {
        EnsureInitialized();

        string[] ids = new string[CardCatalog.DeckSize]; /* 덱 슬롯별 카드 ID */
        for (int i = 0; i < ids.Length; i++)
            ids[i] = PlayerPrefs.GetString($"{DeckKeyPrefix}{i}", CardCatalog.Cards[Mathf.Min(i, CardCatalog.Cards.Length - 1)].Id);

        RepairDuplicateDeckIds(ids);
        return ids;
    }

    /* 기존 저장 데이터에 중복 카드가 있으면 보유 중인 다른 카드로 자동 교체합니다. */
    private static void RepairDuplicateDeckIds(string[] ids)
    {
        HashSet<string> usedIds = new(); /* 현재 덱에서 이미 사용한 카드 ID */
        bool changed = false; /* 저장 데이터 보정 여부 */

        for (int i = 0; i < ids.Length; i++)
        {
            if (IsOwned(ids[i]) && usedIds.Add(ids[i]))
                continue;

            string replacementId = FindFirstOwnedUnusedCardId(usedIds);
            if (string.IsNullOrEmpty(replacementId))
                continue;

            ids[i] = replacementId;
            usedIds.Add(replacementId);
            SetDeckCard(i, replacementId);
            changed = true;
        }

        if (changed)
            PlayerPrefs.Save();
    }

    /* 덱에 아직 들어가지 않은 보유 카드 ID를 찾습니다. */
    private static string FindFirstOwnedUnusedCardId(HashSet<string> usedIds)
    {
        foreach (CardCatalogEntry card in CardCatalog.Cards)
        {
            if (!IsOwned(card.Id) || usedIds.Contains(card.Id))
                continue;

            return card.Id;
        }

        return string.Empty;
    }

    /* 덱 편집 UI에서 선택한 슬롯에 특정 보유 카드를 직접 배치합니다. */
    public static void SetDeckCardToSlot(int slotIndex, string id)
    {
        EnsureInitialized();

        if (slotIndex < 0 || slotIndex >= CardCatalog.DeckSize)
            return;

        if (!IsOwned(id))
            return;

        if (IsCardEquippedInAnotherSlot(id, slotIndex))
            return;

        SetDeckCard(slotIndex, id);
        PlayerPrefs.Save();
    }

    /* 지정한 카드가 현재 덱에 들어가 있는지 확인합니다. */
    public static bool IsCardInDeck(string id)
    {
        return GetDeckSlotIndex(id) >= 0;
    }

    /* 지정한 카드가 특정 슬롯이 아닌 다른 덱 슬롯에 들어가 있는지 확인합니다. */
    public static bool IsCardEquippedInAnotherSlot(string id, int slotIndex)
    {
        string[] deckIds = GetDeckIds();
        for (int i = 0; i < deckIds.Length; i++)
        {
            if (i == slotIndex)
                continue;

            if (deckIds[i] == id)
                return true;
        }

        return false;
    }

    /* 카드 ID가 들어간 덱 슬롯 인덱스를 반환합니다. 없으면 -1입니다. */
    public static int GetDeckSlotIndex(string id)
    {
        string[] deckIds = GetDeckIds();
        for (int i = 0; i < deckIds.Length; i++)
        {
            if (deckIds[i] == id)
                return i;
        }

        return -1;
    }

    /* 이전 방식 호환용으로 슬롯 카드를 다음 보유 카드로 교체합니다. */
    public static void SetNextOwnedCardToDeckSlot(int slotIndex)
    {
        EnsureInitialized();

        if (slotIndex < 0 || slotIndex >= CardCatalog.DeckSize)
            return;

        List<CardCatalogEntry> ownedCards = GetOwnedCards();
        if (ownedCards.Count == 0)
            return;

        string currentId = GetDeckIds()[slotIndex];
        int currentIndex = ownedCards.FindIndex(card => card.Id == currentId);
        int nextIndex = (currentIndex + 1 + ownedCards.Count) % ownedCards.Count;
        SetDeckCard(slotIndex, ownedCards[nextIndex].Id);
        PlayerPrefs.Save();
    }

    /* 도감과 덱 편집에 표시할 보유 카드 목록을 반환합니다. */
    public static List<CardCatalogEntry> GetOwnedCards()
    {
        EnsureInitialized();

        List<CardCatalogEntry> ownedCards = new(); /* 현재 보유 중인 카드 목록 */
        foreach (CardCatalogEntry card in CardCatalog.Cards)
        {
            if (IsOwned(card.Id))
                ownedCards.Add(card);
        }

        return ownedCards;
    }

    public static bool IsOwned(string id) => PlayerPrefs.GetInt($"{OwnedKeyPrefix}{id}", 0) == 1; /* 카드 보유 여부 */
    public static int GetLevel(string id) => Mathf.Max(1, PlayerPrefs.GetInt($"{LevelKeyPrefix}{id}", 1)); /* 카드 레벨 */
    public static int GetExp(string id) => PlayerPrefs.GetInt($"{ExpKeyPrefix}{id}", 0); /* 이전 버전 표시 호환용 경험치 */
    public static int GetGold() => PlayerPrefs.GetInt(GoldKey, 0); /* 현재 보유 골드 */
    public static int GetWins() => PlayerPrefs.GetInt(WinsKey, 0); /* 내부 기록용 승리 수 */
    public static int GetLosses() => PlayerPrefs.GetInt(LossesKey, 0); /* 내부 기록용 패배 수 */
    public static int GetUpgradeCost(string id) => GetLevel(id) * 100; /* 다음 성장에 필요한 골드 */

    /* 보유 골드를 사용해 카드 레벨을 1 올립니다. */
    public static bool TryUpgradeCard(string id, out string message)
    {
        EnsureInitialized();

        if (!IsOwned(id))
        {
            message = "Only owned cards can grow.";
            return false;
        }

        int cost = GetUpgradeCost(id);
        if (GetGold() < cost)
        {
            message = $"성장비 {cost}G가 필요합니다.";
            return false;
        }

        AddGold(-cost);
        SetLevel(id, GetLevel(id) + 1);
        PlayerPrefs.Save();
        message = $"성장 완료: Lv.{GetLevel(id)}";
        return true;
    }

    /* 테스트용으로 카드게임 저장 데이터를 초기 상태로 되돌립니다. */
    public static void ResetProfile()
    {
        foreach (CardCatalogEntry card in CardCatalog.Cards)
        {
            PlayerPrefs.DeleteKey($"{OwnedKeyPrefix}{card.Id}");
            PlayerPrefs.DeleteKey($"{LevelKeyPrefix}{card.Id}");
            PlayerPrefs.DeleteKey($"{ExpKeyPrefix}{card.Id}");
        }

        for (int i = 0; i < CardCatalog.DeckSize; i++)
            PlayerPrefs.DeleteKey($"{DeckKeyPrefix}{i}");

        PlayerPrefs.DeleteKey(InitializedKey);
        PlayerPrefs.DeleteKey(GoldKey);
        PlayerPrefs.DeleteKey(WinsKey);
        PlayerPrefs.DeleteKey(LossesKey);
        EnsureInitialized();
    }

    /* 보유 골드를 증감합니다. 음수는 소비 처리에 사용합니다. */
    private static void AddGold(int amount)
    {
        PlayerPrefs.SetInt(GoldKey, Mathf.Max(0, GetGold() + amount));
    }

    /* 승리 시 미보유 카드 1장을 획득하고, 모두 보유 중이면 골드를 추가 지급합니다. */
    private static string TryUnlockRandomCard()
    {
        List<CardCatalogEntry> lockedCards = new(); /* 아직 획득하지 않은 카드 목록 */
        foreach (CardCatalogEntry card in CardCatalog.Cards)
        {
            if (!IsOwned(card.Id))
                lockedCards.Add(card);
        }

        if (lockedCards.Count == 0)
        {
            AddGold(50);
            return "모든 카드 보유: +50G";
        }

        CardCatalogEntry reward = lockedCards[Random.Range(0, lockedCards.Count)];
        SetOwned(reward.Id, true);
        SetLevel(reward.Id, 1);
        SetExp(reward.Id, 0);
        return $"New card: {reward.CardName}";
    }

    private static void SetOwned(string id, bool owned) => PlayerPrefs.SetInt($"{OwnedKeyPrefix}{id}", owned ? 1 : 0); /* 카드 보유 저장 */
    private static void SetLevel(string id, int level) => PlayerPrefs.SetInt($"{LevelKeyPrefix}{id}", Mathf.Max(1, level)); /* 카드 레벨 저장 */
    private static void SetExp(string id, int exp) => PlayerPrefs.SetInt($"{ExpKeyPrefix}{id}", Mathf.Max(0, exp)); /* 이전 버전 호환용 경험치 저장 */
    private static void SetDeckCard(int slotIndex, string id) => PlayerPrefs.SetString($"{DeckKeyPrefix}{slotIndex}", id); /* 덱 슬롯 저장 */
}
