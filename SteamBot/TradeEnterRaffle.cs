using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot
{
    public class TradeEnterRaffle : TradeSystem
    {
        public int ScrapPutUp;

        public int GetNumScrap()
        {
            int ret = 0;
            foreach (InventoryItem item in OtherAPIbp.result.items)
            {
                if (ItemsList.Contains(item.id))
                {
                    foreach (SchemaItem schemaItem in itemSchema.result.items)
                    {
                        if (item.defindex == schemaItem.defindex)
                        {
                            if (item.defindex == "5000") ret++;
                            else if (item.defindex == "5001") ret += 3;
                            else if (item.defindex == "5002") ret += 9;
                        }
                    }
                }
            }

            return ret;
        }

        public override void OnCleanTrade()
        {
            base.OnCleanTrade();
        }

        public override void OnUserAccept()
        {
            string[] errors2 = VerifyTrade();
            if (errors2.Length > 0)
            {
                sendChat("There were errors in your trade: ");
                foreach (string error in errors2)
                {
                    sendChat(error);
                }
                if (!isAdmin)
                {
                    setReady(false);
                    return;
                }
            }
            if (isConfirmed || isAdmin)
            {
                //Accept It
                dynamic js = acceptTrade();
                if (js.success == true)
                {
                    printConsole("[TradeSystem] Trade was successful!", ConsoleColor.Green);
                }
                else
                {
                    printConsole("[TradeSystem] Trade might have failed.", ConsoleColor.Red);
                }
                OnTradeEnd(true);
                cleanTrade();
            }
            base.OnUserAccept();
        }

        public override void OnTradeEnd(bool success)
        {
            if (success)
            {
                theBot.steamFriends.SendChatMessage(otherSID, EChatEntryType.ChatMsg, "Thanks for entering the raffle!");
                //Here is where you'd have the bot tell your server that somebody entered the raffle.
            }
            base.OnTradeEnd(success);
        }

        public override void OnUserSetReady()
        {
            validateTimer = 0;
            string[] errors2 = VerifyTrade();

            if (errors2.Length > 0)
            {
                sendChat("There were errors in your trade: ");
                foreach (string error in errors2)
                {
                    sendChat(error);
                }
                if (!isAdmin)
                    return;
            }
            setReady(true);
        }

        public override void OnUserAddItem(SchemaItem schemaItem, InventoryItem invItem)
        {
            validateTimer = 2;
            base.OnUserAddItem(schemaItem, invItem);
        }

        public override void OnUserRemoveItem(SchemaItem schemaItem, InventoryItem invItem)
        {
            validateTimer = 2;
            base.OnUserRemoveItem(schemaItem, invItem);
        }

        public override void OnUserSetUnready()
        {
            setReady(false);
        }

        public override void OnUserChat(string message)
        {

            base.OnUserChat(message);
        }

        public override void OnValidate()
        {
            string[] errors2 = VerifyTrade();

            if (errors2.Length > 0)
            {
                sendChat("There were errors in your trade: ");
                foreach (string error in errors2)
                {
                    sendChat(error);
                }
                return;
            }

            base.OnValidate();
        }

        public virtual string[] VerifyTrade()
        {
            ScrapPutUp = 0;

            List<string> errors = new List<string>();

            foreach (InventoryItem item in OtherAPIbp.result.items)
            {
                if (ItemsList.Contains(item.id.ToString()))
                {
                    foreach (SchemaItem schemaItem in itemSchema.result.items)
                    {
                        string type = schemaItem.craft_material_type;
                        string itemid = schemaItem.defindex;
                        if (item.defindex == schemaItem.defindex)
                        {
                            if (item.defindex == "5000") ScrapPutUp++;
                            else if (item.defindex == "5001") ScrapPutUp += 3;
                            else if (item.defindex == "5002") ScrapPutUp += 9;
                            else
                            {
                                errors.Add("Item " + schemaItem.item_name + " is not a metal.");
                            }
                        }
                    }
                }
            }

            if (ScrapPutUp < 1)
            {
                errors.Add("You must put up at least 1 scrap.");
            }

            return errors.ToArray();
        }

        public override void OnSuccessfulInit()
        {
            sendChat("Success. Trade me metal to get a ticket; 1 scrap is 1 ticket.");
            base.OnSuccessfulInit();
        }
    }
}
