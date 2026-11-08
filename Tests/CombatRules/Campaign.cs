using System.Text.Json;

// Tests production progression topology independently of an automated combat policy.
// Battle victory is injected so these checks do not claim to prove game balance.
public static class Campaign
{
    public static int Run()
    {
        int checks=0;
        void Check(bool value,string message) { if(!value)throw new Exception(message); checks++; }
        foreach(int seed in new[]{1,17,456})
        {
            var game=RunEngine.New(seed);
            int battles=0,shops=0,itemRewards=0,cardRewards=0;
            var visited=new HashSet<string>();
            int safety=0;
            while(game.State.Phase!=RunPhase.Victory && ++safety<200)
            {
                var state=game.State;
                switch(state.Phase)
                {
                    case RunPhase.Battle:
                        string encounter=$"{state.Floor}:{state.Node}:{state.Wave}";
                        Check(visited.Add(encounter),"Each battle is entered only once: "+encounter);
                        Check(state.Enemies.Count>=2 && state.Enemies.All(enemy=>enemy.Health>0 && enemy.MaxHealth>=enemy.Health),"Encounter spawns live enemies with valid HP.");
                        int goldBefore=state.Gold, ownedBefore=state.Cards.Count, itemsBefore=state.Items.Sum();
                        bool final=state.Floor==3 && game.IsBoss;
                        state.Enemies.ForEach(enemy=>enemy.Health=0);
                        game.EndTurn(); battles++;
                        if(final)
                        {
                            Check(state.Phase==RunPhase.Victory,"Final boss goes directly to the clear screen.");
                            Check(state.Gold==goldBefore && state.Cards.Count==ownedBefore && state.Items.Sum()==itemsBefore,"Final clear grants no gold, card or item rewards.");
                            Check(!game.ChooseCard(0),"The final clear does not accept a card reward.");
                            game.ChooseItem(0);
                            Check(state.Phase==RunPhase.Victory && state.Items.Sum()==itemsBefore,"The final clear does not accept an item reward.");
                        }
                        else
                        {
                            Check(state.Phase==RunPhase.CardReward,"A nonfinal victory opens its wave reward.");
                            Check(state.Offers.Distinct().Count()==3 && state.Offers.All(id=>id>=0 && id<CardCatalog.All.Length && id!=29),"Wave rewards offer three distinct legal nonultimate cards.");
                        }
                        break;
                    case RunPhase.CardReward:
                        Check(game.ChooseCard(-1),"Wave gold option advances progression."); cardRewards++;
                        break;
                    case RunPhase.ItemReward:
                        int previous=state.Items.Sum(); game.ChooseItem(0); itemRewards++;
                        Check(state.Items.Sum()==previous+1,"A stage item reward grants exactly one item.");
                        break;
                    case RunPhase.Shop:
                        Check(state.Offers.Any(id=>CardCatalog.All[id].Rarity>=2),"Each shop offers at least one gold-or-better card.");
                        int price=game.RerollPrice, gold=state.Gold;
                        Check(game.RerollShop() && state.Gold==gold-price && state.ShopRerolls==1,"Shop reroll spends its advertised price once.");
                        int buy=Enumerable.Range(0,3).First(i=>game.ShopPrice(i)<=state.Gold);
                        gold=state.Gold;
                        Check(game.Buy(buy) && state.Gold<gold && !game.Buy(buy),"Shop purchase spends gold and cannot repeat for the same offer.");
                        game.LeaveShop(); shops++;
                        break;
                    default: throw new Exception("Unexpected campaign phase: "+state.Phase);
                }
            }
            Check(game.State.Phase==RunPhase.Victory && battles==30,"Campaign has exactly 30 battles and reaches Victory.");
            Check(shops==3 && cardRewards==29 && itemRewards==11,"Campaign has three shops, 29 wave card rewards and 11 item rewards; no final-clear rewards.");
            Check(game.State.WavesCleared==30 && game.State.Items.Sum()==11,"Persistent campaign statistics match rewarded progress.");
            var options=new JsonSerializerOptions{IncludeFields=true,IgnoreReadOnlyProperties=true};
            var json=JsonSerializer.Serialize(game.State,options);
            var saved=JsonSerializer.Deserialize<RunState>(json,options)??throw new Exception("Save round trip returned null.");
            var resumed=new RunEngine(saved);
            Check(resumed.State.Phase==RunPhase.Victory && resumed.State.WavesCleared==30 && resumed.State.Items.Sum()==11,"A completed campaign survives a save-data round trip.");
            Console.WriteLine($"Campaign seed {seed}: {battles} battles, {cardRewards} card rewards, {itemRewards} item rewards, {shops} shops.");
        }
        return checks;
    }
}
