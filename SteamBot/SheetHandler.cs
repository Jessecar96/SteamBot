using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using SteamKit2;

namespace SteamBot
{
    class SheetHandler
    {

        /// <summary>
        /// New Method To get Worksheet
        /// </summary>
        /// <returns></returns>
        public WorksheetEntry GetWorksheet(OAuth2Parameters parameters, string IntegrationName, string SpreadSheetURI, SpreadsheetsService service)
        {
          
            OAuthUtil.RefreshAccessToken(parameters);
            string accessToken = parameters.AccessToken;

            GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, IntegrationName, parameters);

            service.RequestFactory = requestFactory;
            SpreadsheetQuery query = new SpreadsheetQuery(SpreadSheetURI);
            SpreadsheetFeed feed = service.Query(query);
            SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];
            WorksheetFeed wsFeed = spreadsheet.Worksheets;
            WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries[0];
            return worksheet;
        }

        //public void UploadSheet(bool Forcesync, Dictionary<string, Tuple<string, SteamID, string, bool>> Maplist, String IntegrationName, string CLIENT_ID,string CLIENT_SECRET, string REDIRECT_URI, string SCOPE, string GoogleAPI)
        public void UploadSheet(bool Forcesync, Dictionary<string, Tuple<string, string, string, bool>> Maplist, OAuth2Parameters parameters, string IntegrationName, SpreadsheetsService service, string SpreadSheetURI)
        {

            WorksheetEntry worksheet = GetWorksheet(parameters, IntegrationName, SpreadSheetURI, service);
            worksheet.Cols = 5;
            worksheet.Rows = Convert.ToUInt32(Maplist.Count + 1);
            worksheet.Update();

            CellQuery cellQuery = new CellQuery(worksheet.CellFeedLink);
            cellQuery.ReturnEmpty = ReturnEmptyCells.yes;
            CellFeed cellFeed = service.Query(cellQuery);
            CellFeed batchRequest = new CellFeed(cellQuery.Uri, service);

            int Entries = 1;

            foreach (var item in Maplist)
            {
                Entries = Entries + 1;
                foreach (CellEntry cell in cellFeed.Entries)
                {
                    if (cell.Title.Text == "A" + Entries.ToString())
                    {
                        cell.InputValue = item.Key;
                    }
                    if (cell.Title.Text == "B" + Entries.ToString())
                    {
                        cell.InputValue = item.Value.Item1;

                    }
                    if (cell.Title.Text == "C" + Entries.ToString())
                    {
                        cell.InputValue = item.Value.Item2.ToString();

                    }
                    if (cell.Title.Text == "D" + Entries.ToString())
                    {
                        cell.InputValue = item.Value.Item3.ToString();

                    }
                    if (cell.Title.Text == "E" + Entries.ToString())
                    {
                        cell.InputValue = item.Value.Item4.ToString();
                    }
                }
            }

            cellFeed.Publish();
            CellFeed batchResponse = (CellFeed)service.Batch(batchRequest, new Uri(cellFeed.Batch));
        }
        public Dictionary<string, Tuple<string, string, string, bool>> SyncSheetDownload(WorksheetEntry Worksheet, SpreadsheetsService service, OAuth2Parameters paramaters)
        {
            
            Dictionary<string, Tuple<string, string, string, bool>> OnlineMapList = new Dictionary<string, Tuple<string, string, string, bool>>();
            int Entries = 1;

            string map = "MapNameError";
            string URL = "URL ERROR";
            string UserSteamID = "0";
            string Note = "No Notes";
            bool MapUploadStatus = false;

            AtomLink listFeedLink = Worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
            ListFeed listFeed = service.Query(listQuery);

            //TODO GENERAL CLEANUP

            foreach (ListEntry row in listFeed.Entries)
            {

                foreach (ListEntry.Custom element in row.Elements)
                {
                    map = row.Elements[0].Value;
                    URL = row.Elements[1].Value;
                    UserSteamID = row.Elements[2].Value;
                    Note = row.Elements[3].Value;

                    if (row.Elements[3].Value == "TRUE")
                    {
                        MapUploadStatus = true;
                    }
                    else
                    {
                        MapUploadStatus = false;
                    }
                    OnlineMapList.Add(map, new Tuple<string, string, string, bool>(URL, UserSteamID, Note, MapUploadStatus));
                }
            }
            return OnlineMapList;

        }
    }
}