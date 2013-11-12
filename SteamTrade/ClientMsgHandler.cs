//Thank you, VoiDeD!

namespace SteamKit2.Internal
{
    class SteamWebAPI : ClientMsgHandler
    {
        public class NonceCallback : CallbackMsg
        {
            public EResult Result { get; set; }
            public string Nonce { get; set; }

            internal NonceCallback(CMsgClientRequestWebAPIAuthenticateUserNonceResponse body)
            {
                Result = (EResult)body.eresult;
                Nonce = body.webapi_authenticate_user_nonce;
            }
        }


        public void RequestNonce()
        {
            var reqMsg = new ClientMsgProtobuf<CMsgClientRequestWebAPIAuthenticateUserNonce>(EMsg.ClientRequestWebAPIAuthenticateUserNonce);
            Client.Send(reqMsg);
        }


        public override void HandleMsg(IPacketMsg packetMsg)
        {
            switch (packetMsg.MsgType)
            {
                case EMsg.ClientRequestWebAPIAuthenticateUserNonceResponse:
                    HandleWebAPINonce(packetMsg);
                    break;
            }
        }

        void HandleWebAPINonce(IPacketMsg packetMsg)
        {
            var nonceMsg = new ClientMsgProtobuf<CMsgClientRequestWebAPIAuthenticateUserNonceResponse>(packetMsg);

            var callback = new NonceCallback(nonceMsg.Body);
            Client.PostCallback(callback);
        }
    }
}
