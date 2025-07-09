using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.ModelBinding;
using System.Web.UI;
using System.Xml.Linq;
using AddonProcurement.GRServiceReference;
using AddonProcurement.Helpers;
using AddonProcurement.Managers;
using AddonProcurement.Models;
using AddonProcurement.Models.CustomEntity;
using AddonProcurement.Models.CustomEntity.Models;
using AddonProcurement.Models.RequestModel;
using AddonProcurement.Models.ResponeModel;
using AddonProcurement.VendorMasterServiceReference;
using WolfApprove.Model;
using WolfApprove.Model.CustomClass;
using WolfApprove.Model.Extension;
using WolfApprove.Model.Migrations;
using static AddonProcurement.Models.RequestModel.RequestCustomeMasterData;

namespace AddonProcurement
{
    [RoutePrefix("api/v1/addonThaiLife")]
    public class AddonController : ApiController
    {
        [HttpPost]
        [Route("StartAddon")]
        public IHttpActionResult StartAddon()
        {
            //var i = new InsertTRNMemoForm();
            //var x = i.InsertTRNFormTest(7392, @"data source=172.22.10.148;initial catalog=WolfApproveCore.thailife;persist security info=True;user id=wolf-admin;password=UZPj&9e7Hh2)hH98I;");
            return Ok();
        }

        [HttpPost]
        [Route("InsertVendorMaster")]
        public IHttpActionResult InsertVendorMaster(RequestInputVendorMaster requestBody)
        {
            var request = new DT_TLI_API003_SNDRow();
            try
            {
                // fill data state 1
                var jsonHeader = JsonConvert.SerializeObject(requestBody.Header);
                request = JsonConvert.DeserializeObject<DT_TLI_API003_SNDRow>(jsonHeader);
                request = SetFieldsToEmptyIfNull(request);
                // fill data state 2
                foreach (var item in requestBody.WHTItems)
                {
                    string type = "";
                    if (!string.IsNullOrEmpty(item.WHTType))
                        //type = item.WHTType.ToUpper().Replace(" ", "");
                        type = item.WHTTypeDetail.ToUpper().Replace(" ", "");
                    switch (type)
                    {
                        case "WHTTYPE1":
                            request.WHTType1 = item.WHTType;
                            if (!string.IsNullOrEmpty(item.WHTCode))
                                request.WHTCode1 = item.WHTCode.Split(':')[0].Trim();
                            request.TypeOfRecipient1 = item.TypeOfRecipient;
                            request.SubjectTax1 = item.SubjectTax;
                            break;

                        case "WHTTYPE2":
                            request.WHTType2 = item.WHTType;
                            if (!string.IsNullOrEmpty(item.WHTCode))
                                request.WHTCode2 = item.WHTCode.Split(':')[0].Trim();
                            request.TypeOfRecipient2 = item.TypeOfRecipient;
                            request.SubjectTax2 = item.SubjectTax;
                            break;

                        case "WHTTYPE3":
                            request.WHTType3 = item.WHTType;
                            if (!string.IsNullOrEmpty(item.WHTCode))
                                request.WHTCode3 = item.WHTCode.Split(':')[0].Trim();
                            request.TypeOfRecipient3 = item.TypeOfRecipient;
                            request.SubjectTax1 = item.SubjectTax;
                            break;

                        case "WHTTYPE4":
                            request.WHTType4 = item.WHTType;
                            if (!string.IsNullOrEmpty(item.WHTCode))
                                request.WHTCode4 = item.WHTCode.Split(':')[0].Trim();
                            request.TypeOfRecipient4 = item.TypeOfRecipient;
                            request.SubjectTax4 = item.SubjectTax;
                            break;

                        default:
                            break;
                    }
                }
                //WriteLogFile.LogAddon($"Fill data :{request.ToJson()}");
                var soapResult = AddonManager.InsertVendorMaster(request);
                string statusType = soapResult.ToList().First().TYPE;
                string msgs = string.Join(",", soapResult.Select(s => s.MESSAGE));
                var result = new
                {
                    Type = statusType,
                    messageType = statusType.ToLower() == "e" ? "Error" : "",
                    status = statusType,
                    Types = string.Join(",", soapResult.Select(s => s.TYPE)),
                    Messages = msgs,
                    Numbers = string.Join(",", soapResult.Select(s => s.NUMBER)),
                    Ids = string.Join(",", soapResult.Select(s => s.ID)),
                    soapReturn = soapResult.ToList()
                };
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private DT_TLI_API003_SNDRow SetFieldsToEmptyIfNull(DT_TLI_API003_SNDRow obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string) && field.GetValue(obj) == null)
                {
                    field.SetValue(obj, string.Empty);
                }
            }
            return obj;
        }

        [HttpPost]
        [Route("InsertGRMaster")]
        public IHttpActionResult InsertGRMaster(RequestInputGRMaster request)
        {
            var prePareData = new List<DT_TLI_AAI001_SNDRow>();
            String sMT_CMAlert_MasterType = "CMAlert";
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var _ActivePostingDateToDay = WebConfigurationManager.AppSettings["ActivePostingDateToDay"];
            Boolean blActivePostingDateToDay = false;
            if (!String.IsNullOrEmpty(_ActivePostingDateToDay))
                blActivePostingDateToDay = _ActivePostingDateToDay == "T";

            var _ActiveGRGenAssetType2 = WebConfigurationManager.AppSettings["ActiveGRGenAssetType2"];
            Boolean blActiveGRGenAssetType2 = false;
            if (!String.IsNullOrEmpty(_ActiveGRGenAssetType2))
                blActiveGRGenAssetType2 = _ActiveGRGenAssetType2 == "T";

            Boolean blActiveCheckDEBITCREDIT = false;
            Boolean blActiveNewFormula = false;

            MSTMasterData iGRPOSTSAP = new MSTMasterData();
            MSTMasterData iGRDEBITCREDIT = new MSTMasterData();

            List<MSTMasterData> lst_GRGAT = new List<MSTMasterData>();
            List<MSTMasterData> lst_CMAlert = new List<MSTMasterData>();

            String sAssetType = String.Empty; //AssetType ที่ระบบ run ไปล่าสุด ภายใต้เลขที่ PO No
            String sMT_GRGenAssetType = "GRGAT"; //Master Type > Master Data
            String sPONo = String.Empty; //เลขที่ PO
            String sGRNo = String.Empty; //รับครั้งที่
            String sRunning = String.Empty; //ลำดับที่ run ไปล่าสุดภายใต้เลขที่ PO No
            String sPrefix = String.Empty; //Prefix ที่เป็นตัวกำหนดการ run ไปล่าสุด

            string[] Asset = new string[] { "01", "02", "03", "04", "05", "06", "07", "08" };

            String sMT_GRGenAssetType2 = "GRGAT2"; //Master Type > Master Data
            List<MSTMasterData> lst_GRGAT2 = new List<MSTMasterData>();
            List<MSTMasterData> lst_GRGAT2_Insert = new List<MSTMasterData>();

            try
            {
                //case 1 KO expense
                DT_TLI_AAI001_SNDRowCustom apCheck = request.AAItems.FirstOrDefault();

                if (apCheck.BLART.ToUpper() == "KO")
                {
                    // add aa
                    var jsonAA = JsonConvert.SerializeObject(request.AAItems);
                    var AAItems = JsonConvert.DeserializeObject<List<DT_TLI_AAI001_SNDRow>>(jsonAA);
                    prePareData.AddRange(AAItems);
                }
                else if (blActiveGRGenAssetType2 == false && apCheck.BLART.ToUpper() == "KJ")
                {
                    GEN_AND_CONTROL_ASSETTYPE(_context, apCheck, sMT_CMAlert_MasterType, request, sMT_GRGenAssetType,
                       Asset,
                       ref prePareData, ref blActiveCheckDEBITCREDIT, ref blActiveNewFormula,
                       ref iGRPOSTSAP, ref iGRDEBITCREDIT,
                       ref sAssetType, ref sPONo, ref sGRNo, ref sRunning, ref sPrefix,
                       ref lst_GRGAT, ref lst_CMAlert
                       );
                }
                else
                {
                    GEN_AND_CONTROL_ASSETTYPE_2(_context, apCheck, sMT_CMAlert_MasterType, request, sMT_GRGenAssetType2,
                        Asset,
                        ref prePareData, ref blActiveCheckDEBITCREDIT, ref blActiveNewFormula,
                        ref iGRPOSTSAP, ref iGRDEBITCREDIT,
                        ref sAssetType, ref sPONo, ref sRunning, ref sPrefix,
                        ref lst_GRGAT2, ref lst_CMAlert,
                        ref lst_GRGAT2_Insert);
                }

                //add ap

                var jsonfirstRowAPItems = JsonConvert.SerializeObject(request.APItems.FirstOrDefault());
                var firstRowAPItems = JsonConvert.DeserializeObject<DT_TLI_AAI001_SNDRow>(jsonfirstRowAPItems);
                firstRowAPItems.XREF1 = (prePareData.Count + 1).ToString();

                if (prePareData.Count > 0 && blActiveCheckDEBITCREDIT)
                {
                    Decimal iAA = prePareData.Sum(a => decimal.Parse(String.IsNullOrEmpty(a.WRBTR) ? "0" : a.WRBTR));
                    Decimal iAP = decimal.Parse(String.IsNullOrEmpty(firstRowAPItems.WRBTR) ? "0" : firstRowAPItems.WRBTR);
                    #region[check balance Debit Credit]

                    if (iAA != iAP)
                    {
                        WriteLogFile.LogAddon($"iAA {iAA.ToString("#,##0.00")} | iAP {iAP.ToString("#,##0.00")}");
                        WriteLogFile.LogAddon($"blActiveNewFormula({blActiveNewFormula})");

                        if (blActiveNewFormula)
                        {
                            if (prePareData.Count > 0)
                            {
                                Decimal i_sum_AA_X_1 = 0;
                                for (int i = 0; i < prePareData.Count - 1; i++)
                                {
                                    i_sum_AA_X_1 += decimal.Parse(String.IsNullOrEmpty(prePareData[i].WRBTR) ? "0" : prePareData[prePareData.Count - 1].WRBTR);
                                }
                                WriteLogFile.LogAddon($"i_sum_AA_X_1 {i_sum_AA_X_1.ToString("#,##0.00")}");
                                Decimal iLastAA = iAP - i_sum_AA_X_1;
                                prePareData[prePareData.Count - 1].WRBTR = iLastAA.ToString("#.#0");
                                prePareData[prePareData.Count - 1].PUBTR = iLastAA.ToString("#.#0");
                                WriteLogFile.LogAddon($"iLastAA WRBTR({prePareData[prePareData.Count - 1].WRBTR}) PUBTR({prePareData[prePareData.Count - 1].PUBTR})");
                            }
                            iAA = prePareData.Sum(a => decimal.Parse(String.IsNullOrEmpty(a.WRBTR) ? "0" : a.WRBTR));
                        }

                        WriteLogFile.LogAddon($"blActiveNewFormula({blActiveNewFormula}) iAA {iAA.ToString("#,##0.00")} | iAP {iAP.ToString("#,##0.00")}");
                        WriteLogFile.LogAddon($"blActiveNewFormula({blActiveNewFormula}) call SI_TLI_PurchaseOnline_SYNC_SND:{prePareData.ToArray().ToJson()}");

                        if (iAA != iAP)
                        {
                            String sValue3 = iGRDEBITCREDIT.Value3;
                            sValue3 = sValue3.Replace("[DEBIT]", iAA.ToString("#,##0.00"))
                                .Replace("[CREDIT]", iAP.ToString("#,##0.00"));
                            List<ErrorDetail> temp = new List<ErrorDetail>();
                            var customResponse = new InsertGRCustomResponeModel();
                            temp.Add(new ErrorDetail { Type = "E", Message = sValue3 });
                            customResponse.@return = temp;
                            customResponse.Types = "E";
                            customResponse.Message = sValue3;
                            customResponse.Number = iGRDEBITCREDIT.Value1;
                            return Ok(customResponse);
                        }
                    }
                    #endregion
                }

                prePareData.Add(firstRowAPItems);
                if (blActivePostingDateToDay)
                {
                    foreach (var i in prePareData)
                    {
                        i.BUDAT = DateTime.Now.ToString("yyyyMMdd");
                    }
                }
                var result = AddonManager.InsertGRMaster(prePareData.ToArray());

                #region[สำหรับการต่อ Msg Alert จาก SAP กรณีที่ได้ Msg Type ไม่เท่ากับ S เพื่อนำข้อความใน Value3 ไปต่อท้ายข้อความที่ได้มาจากฝั่ง SAP]
                WriteLogFile.LogAddon($"iGRPOSTSAP.Value3 : {iGRPOSTSAP.Value3}");
                if (result != null && result?.Type?.ToLower() != "s")
                {
                    List<ErrorDetail> temp = result.@return;
                    temp.Add(new ErrorDetail { Type = "E", Message = iGRPOSTSAP.Value3 });
                    result.@return = temp;
                    result.Message = result.Message += iGRPOSTSAP.Value3;
                    WriteLogFile.LogAddon($"result.Message : {result.Message}");
                }
                #endregion

                #region[after post SAP > insert of update Master Data GRGAT for running Asset Type]
                WriteLogFile.LogAddon($"before > after post SAP > insert of update Master Data GRGAT for running Asset Type");
                if (result != null && result?.Type?.ToLower() == "s")
                {
                    WriteLogFile.LogAddon($"sAssetType : {sAssetType}");
                    WriteLogFile.LogAddon($"blActiveGRGenAssetType2 : {blActiveGRGenAssetType2}");

                    if (!String.IsNullOrEmpty(sAssetType))
                    {
                        if (blActiveGRGenAssetType2)
                        {
                            WriteLogFile.LogAddon($"lst_GRGAT.Count : {lst_GRGAT2.Count}");
                            WriteLogFile.LogAddon($"lst_GRGAT2_Insert.Count : {lst_GRGAT2_Insert.Count}");
                            WriteLogFile.LogAddon($"sMT_GRGenAssetType2 : {sMT_GRGenAssetType2}");
                            WriteLogFile.LogAddon($"sPONo : {sPONo}");
                            WriteLogFile.LogAddon($"sRunning : {sRunning}");
                            WriteLogFile.LogAddon($"sPrefix : {sPrefix}");

                            if (lst_GRGAT2_Insert.Count > 0)
                            {
                                foreach (MSTMasterData i in lst_GRGAT2_Insert)
                                {
                                    _context.MSTMasterDatas.Add(i);
                                }
                                _context.SaveChanges();
                            }
                        }
                        else
                        {
                            WriteLogFile.LogAddon($"lst_GRGAT.Count : {lst_GRGAT.Count}");
                            WriteLogFile.LogAddon($"sMT_GRGenAssetType : {sMT_GRGenAssetType}");
                            WriteLogFile.LogAddon($"sPONo : {sPONo}");
                            WriteLogFile.LogAddon($"sGRNo : {sGRNo}");
                            WriteLogFile.LogAddon($"sRunning : {sRunning}");
                            WriteLogFile.LogAddon($"sPrefix : {sPrefix}");

                            if (lst_GRGAT.Count == 0)
                            {
                                MSTMasterData iGRGAT = new MSTMasterData
                                {
                                    MasterType = sMT_GRGenAssetType,
                                    Value1 = sPONo,
                                    Value2 = sGRNo,
                                    Value3 = sRunning,
                                    Value4 = sPrefix,
                                    Value5 = sAssetType,
                                    IsActive = true,
                                    CreatedDate = DateTime.Now,
                                    ModifiedDate = DateTime.Now
                                };
                                _context.MSTMasterDatas.Add(iGRGAT);
                                _context.SaveChanges();
                            }
                            else
                            {
                                var iGRGAT = lst_GRGAT.First();
                                iGRGAT.Value2 = sGRNo;
                                iGRGAT.Value3 = sRunning;
                                iGRGAT.Value5 = sAssetType;
                                _context.SaveChanges();
                            }
                        }
                    }
                }
                WriteLogFile.LogAddon($"after > after post SAP > insert of update Master Data GRGAT for running Asset Type");
                #endregion

                #region[Paid Method]
                if (result != null && result?.Type?.ToLower() == "s")
                {
                    var transactions = new List<BudgetTransaction>();
                    foreach (var item in request.Orders)
                    {
                        var transaction = new BudgetTransaction
                        {
                            BudgetCostCenter = item.Costcenter,
                            GLAccount = item.GLAccount,
                            InternalOrder = item.IO,
                            TransactionType = BudgetType.Paid,
                            Amount = decimal.Parse(item.Amount),
                            DocumentNo = request.Header.DocumentNo,
                            Description = "",
                            BudgetYear = (int)decimal.Parse(item.BudgetYear),
                            TransactionDate = DateTime.Now
                        };

                        transactions.Add(transaction);
                    }

                    _context.BudgetTransactions.AddRange(transactions);
                    _context.SaveChanges();
                }
                #endregion

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                WriteLogFile.LogAddon(ex.ToJson());

                var lst_temp = _context.MSTMasterDatas.Where(a => a.MasterType == sMT_CMAlert_MasterType && a.IsActive == true &&
                    a.Value1 == "GRCATCH").ToList();

                WriteLogFile.LogAddon($"lst_temp.Count ({lst_temp.Count})");
                if (lst_temp.Count > 0)
                {
                    List<ErrorDetail> temp = new List<ErrorDetail>();
                    var customResponse = new InsertGRCustomResponeModel();
                    temp.Add(new ErrorDetail { Type = "E", Message = $"{ex.ToString()} {lst_temp.First().Value3}" });
                    customResponse.@return = temp;
                    customResponse.Types = "E";
                    customResponse.Message = lst_temp.First().Value3;
                    customResponse.Number = lst_temp.First().Value1;
                    return Ok(customResponse);
                }
                else
                {
                    return InternalServerError(ex);
                }
            }
        }

        public void GEN_AND_CONTROL_ASSETTYPE(TLIContext _context, DT_TLI_AAI001_SNDRowCustom apCheck,
            String sMT_CMAlert_MasterType, RequestInputGRMaster request, String sMT_GRGenAssetType,
            string[] Asset,
            ref List<DT_TLI_AAI001_SNDRow> prePareData, ref Boolean blActiveCheckDEBITCREDIT, ref Boolean blActiveNewFormula,
            ref MSTMasterData iGRPOSTSAP, ref MSTMasterData iGRDEBITCREDIT,
            ref String sAssetType, ref String sPONo, ref String sGRNo, ref String sRunning, ref String sPrefix,
            ref List<MSTMasterData> lst_GRGAT, ref List<MSTMasterData> lst_CMAlert
            )
        {
            WriteLogFile.LogAddon($"Start GEN_AND_CONTROL_ASSETTYPE");

            //Case Asset Code 999 and Asset Code 999
            Boolean IsAssetCode999 = false;
            Boolean IsAssetCodeHaveIO = false;
            Check_IsAssetCode999_And_IsAssetCodeHaveIO(apCheck, ref IsAssetCode999, ref IsAssetCodeHaveIO);
            //Case Asset Code 999 and Asset Code 999

            //add aa
            int rowIndex = 0;
            int iGRNo = 0;

            String tempPONo = sPONo = apCheck.BKTXT; //เลขที่ PO

            lst_GRGAT = _context.MSTMasterDatas.Where(a => a.MasterType == sMT_GRGenAssetType && a.IsActive == true &&
                a.Value1 == tempPONo).ToList();

            Check_ActiveCheckDEBITCREDIT_And_ActiveNewFormula(_context, sMT_CMAlert_MasterType,
                ref lst_CMAlert, ref iGRPOSTSAP, ref iGRDEBITCREDIT, ref blActiveCheckDEBITCREDIT, ref blActiveNewFormula);

            int iRunning = 0;
            if (lst_GRGAT.Count > 0)
            {
                String sValue2 = String.IsNullOrEmpty(lst_GRGAT.First().Value2) ? "0" : lst_GRGAT.First().Value2;
                iGRNo = (int)decimal.Parse(sValue2);

                String sValue3 = String.IsNullOrEmpty(lst_GRGAT.First().Value3) ? "0" : lst_GRGAT.First().Value3;
                iRunning = (int)decimal.Parse(sValue3);
            }

            iGRNo++;
            sGRNo = iGRNo.ToString("D2");
            sPrefix = $"{sPONo}-{sGRNo}";

            foreach (var item in request.AAItems)
            {
                var json = JsonConvert.SerializeObject(item);

                if (Asset.Contains(item.assetgroup))
                {
                    int iLoop = 1;
                    decimal qunty = decimal.Parse(item.QUNTY);
                    if (qunty % 1 == 0)
                        iLoop = (int)decimal.Parse(item.QUNTY);

                    WriteLogFile.LogAddon($"item.QUNTY = {item.QUNTY}");
                    WriteLogFile.LogAddon($"iLoop = {iLoop}");

                    for (int i = 0; i < iLoop; i++)
                    {
                        var duItem = JsonConvert.DeserializeObject<DT_TLI_AAI001_SNDRow>(json);

                        if (IsAssetCode999 || IsAssetCodeHaveIO)
                        {
                            //If AssetCode = 999 and Asset have IO send QUNTY from Screen but interface 1 line
                            /* 2024-11-29 Fixed CAPEX CASE 3 : PUBTR 171200 > 17120
                                * duItem.WRBTR = DTO_WRBTR_X(duItem.WRBTR, duItem.QUNTY);
                            duItem.PUBTR = duItem.WRBTR;*/
                            if (IsAssetCode999)
                                duItem.PUBTR = DTO_WRBTR_D(duItem.PUBTR, duItem.QUNTY);
                            iLoop = 1;
                        }
                        else
                        {
                            //Fixed Issue 2025-01-06
                            //ไม่ใช่ XX999 หรือไม่มี IO ต้องแตกรายการ และส่งราคาต่อหน่วยมา ไม่ใช่ราคารวม
                            duItem.WRBTR = DTO_WRBTR_D(duItem.WRBTR, iLoop.ToString());
                            duItem.PUBTR = DTO_WRBTR_D(duItem.PUBTR, duItem.QUNTY);
                            //Fixed Issue 2025-01-06
                            duItem.QUNTY = "1";
                        }

                        rowIndex++;
                        iRunning++;
                        sRunning = iRunning.ToString("D3");
                        sAssetType = $"{sPrefix}{sRunning}";
                        duItem.XREF1 = rowIndex.ToString();
                        duItem.TYPBZ = sAssetType;
                        prePareData.Add(duItem);
                        WriteLogFile.LogAddon($"sAssetType = {sAssetType}");
                    }
                }
                else
                {
                    var duItem = JsonConvert.DeserializeObject<DT_TLI_AAI001_SNDRow>(json);
                    prePareData.Add(duItem);
                }
            }

            WriteLogFile.LogAddon($"End GEN_AND_CONTROL_ASSETTYPE");
        }

        public void GEN_AND_CONTROL_ASSETTYPE_2(TLIContext _context, DT_TLI_AAI001_SNDRowCustom apCheck,
            String sMT_CMAlert_MasterType, RequestInputGRMaster request, String sMT_GRGenAssetType2,
            string[] Asset, ref List<DT_TLI_AAI001_SNDRow> prePareData,
            ref Boolean blActiveCheckDEBITCREDIT, ref Boolean blActiveNewFormula,
            ref MSTMasterData iGRPOSTSAP, ref MSTMasterData iGRDEBITCREDIT,
            ref String sAssetType, ref String sPONo, ref String sRunning, ref String sPrefix,
            ref List<MSTMasterData> lst_GRGAT2, ref List<MSTMasterData> lst_CMAlert,
            ref List<MSTMasterData> lst_GRGAT2_Insert)
        {
            WriteLogFile.LogAddon($"Start GEN_AND_CONTROL_ASSETTYPE_2");

            //Case Asset Code 999 and Asset have IO
            Boolean IsAssetCode999 = false;
            Boolean IsAssetCodeHaveIO = false;
            Check_IsAssetCode999_And_IsAssetCodeHaveIO(apCheck, ref IsAssetCode999, ref IsAssetCodeHaveIO);
            //Case Asset Code 999 and Asset have IO

            //add aa
            int rowIndex = 0;

            String tempPONo = sPONo = apCheck.BKTXT; //เลขที่ PO
            String tempInvoiceNo = apCheck.XBLNR; //Invoice No
            String tempGRDocumentNo = request.Header.DocumentNo; //GR Document No

            WriteLogFile.LogAddon($"sPONo = {sPONo} | tempInvoiceNo = {tempInvoiceNo} | tempGRDocumentNo = {tempGRDocumentNo}");

            List<MSTMasterData> vGRGAT2 = _context.Database.SqlQuery<MSTMasterData>("Select * from dbo.ViewGRGAT2")
                .Where(x => x.MasterType == sMT_GRGenAssetType2 && x.Value1 == tempPONo).ToList();

            WriteLogFile.LogAddon($"vGRGAT2.Count = {vGRGAT2.Count}");
            foreach (var i in vGRGAT2)
            {
                lst_GRGAT2.Add(new MSTMasterData
                {
                    MasterId = i.MasterId,
                    MasterType = i.MasterType,
                    Value1 = i.Value1,
                    Value2 = i.Value2,
                    Value3 = i.Value3,
                    Value4 = i.Value4,
                    Value5 = i.Value5,
                    IsActive = i.IsActive,
                    CreatedBy = i.CreatedBy,
                    CreatedDate = i.CreatedDate,
                    ModifiedBy = i.ModifiedBy,
                    ModifiedDate = i.ModifiedDate,
                    Seq = i.Seq
                });
            }
            WriteLogFile.LogAddon($"lst_GRGAT2.Count = {lst_GRGAT2.Count}");

            Check_ActiveCheckDEBITCREDIT_And_ActiveNewFormula(_context, sMT_CMAlert_MasterType,
                ref lst_CMAlert, ref iGRPOSTSAP, ref iGRDEBITCREDIT, ref blActiveCheckDEBITCREDIT, ref blActiveNewFormula);

            lst_CMAlert = _context.MSTMasterDatas.Where(a => a.MasterType == sMT_CMAlert_MasterType && a.IsActive == true).ToList();

            sPrefix = $"{sPONo}-";
            String tempItemID = String.Empty;
            int iRunning = 0;

            foreach (var item in request.AAItems)
            {
                var json = JsonConvert.SerializeObject(item);

                if (Asset.Contains(item.assetgroup))
                {
                    int iLoop = 1;
                    decimal qunty = decimal.Parse(item.QUNTY);
                    if (qunty % 1 == 0)
                        iLoop = (int)decimal.Parse(item.QUNTY);

                    WriteLogFile.LogAddon($"item.QUNTY = {item.QUNTY}");
                    WriteLogFile.LogAddon($"iLoop = {iLoop}");

                    if (item.CODE != tempItemID)
                    {
                        tempItemID = item.CODE;
                        iRunning = 0;
                    }
                    WriteLogFile.LogAddon($"item.CODE = {item.CODE} | tempItemID = {tempItemID}");

                    for (int i = 0; i < iLoop; i++)
                    {
                        var duItem = JsonConvert.DeserializeObject<DT_TLI_AAI001_SNDRow>(json);

                        if (IsAssetCode999 || IsAssetCodeHaveIO)
                        {
                            //If AssetCode = 999 and Asset have IO send QUNTY from Screen but interface 1 line
                            /* 2024-11-29 Fixed CAPEX CASE 3 : PUBTR 171200 > 17120
                                * duItem.WRBTR = DTO_WRBTR_X(duItem.WRBTR, duItem.QUNTY);
                            duItem.PUBTR = duItem.WRBTR;*/
                            if (IsAssetCode999)
                                duItem.PUBTR = DTO_WRBTR_D(duItem.PUBTR, duItem.QUNTY);
                            iLoop = 1;
                        }
                        else
                        {
                            //Fixed Issue 2025-01-06
                            //ไม่ใช่ XX999 หรือไม่มี IO ต้องแตกรายการ และส่งราคาต่อหน่วยมา ไม่ใช่ราคารวม
                            duItem.WRBTR = DTO_WRBTR_D(duItem.WRBTR, iLoop.ToString());
                            duItem.PUBTR = DTO_WRBTR_D(duItem.PUBTR, duItem.QUNTY);
                            //Fixed Issue 2025-01-06
                            duItem.QUNTY = "1";
                        }

                        rowIndex++;
                        iRunning++;
                        sRunning = iRunning.ToString("D4");
                        sAssetType = $"{sPrefix}{sRunning}";
                        sAssetType = LoopGenAssetType(sAssetType, lst_GRGAT2, sPrefix, tempItemID, ref iRunning);

                        var tempGRGAT2 = new MSTMasterData
                        {
                            MasterType = sMT_GRGenAssetType2,
                            Value1 = tempPONo,
                            Value2 = tempItemID,
                            Value3 = tempInvoiceNo,
                            Value4 = tempGRDocumentNo,
                            Value5 = sAssetType,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now
                        };

                        lst_GRGAT2_Insert.Add(tempGRGAT2);
                        lst_GRGAT2.Add(tempGRGAT2);

                        duItem.XREF1 = rowIndex.ToString();
                        duItem.TYPBZ = sAssetType;
                        prePareData.Add(duItem);
                        WriteLogFile.LogAddon($"sAssetType = {sAssetType}");
                    }
                }
                else
                {
                    var duItem = JsonConvert.DeserializeObject<DT_TLI_AAI001_SNDRow>(json);
                    prePareData.Add(duItem);
                }
            }

            WriteLogFile.LogAddon($"End GEN_AND_CONTROL_ASSETTYPE_2");
        }

        public String LoopGenAssetType(String sAssetType, List<MSTMasterData> lst_GRGAT2, String sPrefix, String tempItemID, ref int iRunning)
        {
            WriteLogFile.LogAddon($"sAssetType = {sAssetType} | sPrefix = {sPrefix} | tempItemID = {tempItemID} | iRunning = {iRunning}");

            var temp1 = lst_GRGAT2.Where(a => a.Value5 == sAssetType && a.Value2 == tempItemID && a.IsActive == false).ToList();
            WriteLogFile.LogAddon($"1 sAssetType = {sAssetType} ");
            if (temp1.Count > 0)
            {
                WriteLogFile.LogAddon($"1.1 sAssetType = {sAssetType} ");
                var temp11 = lst_GRGAT2.Where(a => a.Value5 == sAssetType && a.Value2 == tempItemID && a.IsActive == true).ToList();
                if (temp11.Count == 0)
                {
                    WriteLogFile.LogAddon($"1.1.1 sAssetType = {sAssetType} ");
                    return sAssetType;
                }
            }

            WriteLogFile.LogAddon($"2 sAssetType = {sAssetType} ");
            var temp2 = lst_GRGAT2.Where(a => a.Value5 == sAssetType).ToList();
            if (temp2.Count > 0)
            {
                WriteLogFile.LogAddon($"2.1 sAssetType = {sAssetType} ");
                iRunning++;
                String sRunning = iRunning.ToString("D4");
                sAssetType = $"{sPrefix}{sRunning}";
                if (iRunning <= 9999)
                {
                    WriteLogFile.LogAddon($"2.1.1 sAssetType = {sAssetType}");
                    return LoopGenAssetType(sAssetType, lst_GRGAT2, sPrefix, tempItemID, ref iRunning);
                }
            }

            WriteLogFile.LogAddon($"3 sAssetType = {sAssetType} ");

            return sAssetType;
        }

        //public String LoopGenAssetType(String sAssetType, List<MSTMasterData> lst_GRGAT2, String sPrefix, String tempItemID, ref int iRunning)
        //{
        //    WriteLogFile.LogAddon($"sAssetType = {sAssetType} | sPrefix = {sPrefix} | tempItemID = {tempItemID} | iRunning = {iRunning}");

        //    Boolean blLoop = false;

        //    var temp1 = lst_GRGAT2.Where(a => a.Value5 == sAssetType && a.Value2 == tempItemID && a.IsActive == false).ToList();
        //    WriteLogFile.LogAddon($"1 sAssetType = {sAssetType} ");
        //    if (temp1.Count > 0)
        //    {
        //        WriteLogFile.LogAddon($"1.1 sAssetType = {sAssetType} ");
        //        var temp11 = lst_GRGAT2.Where(a => a.Value5 == sAssetType && a.Value2 == tempItemID && a.IsActive == true).ToList();
        //        if (temp11.Count == 0)
        //        {
        //            WriteLogFile.LogAddon($"1.1.1 sAssetType = {sAssetType} ");
        //            return sAssetType;
        //        }

        //        blLoop = true;

        //    }

        //    WriteLogFile.LogAddon($"2 sAssetType = {sAssetType} ");
        //    if (blLoop == false)
        //    {
        //        WriteLogFile.LogAddon($"2.1 sAssetType = {sAssetType} ");
        //        var temp2 = lst_GRGAT2.Where(a => a.Value5 == sAssetType && a.Value2 != tempItemID).ToList();
        //        if (temp2.Count > 0)
        //        {
        //            WriteLogFile.LogAddon($"2.1.1 sAssetType = {sAssetType} ");
        //            blLoop = true;
        //        }
        //    }

        //    WriteLogFile.LogAddon($"3 sAssetType = {sAssetType} ");
        //    if (blLoop)
        //    {
        //        WriteLogFile.LogAddon($"3.1 sAssetType = {sAssetType} ");
        //        iRunning++;
        //        String sRunning = iRunning.ToString("D4");
        //        sAssetType = $"{sPrefix}{sRunning}";
        //        if (iRunning <= 9999)
        //        {
        //            WriteLogFile.LogAddon($"3.1.1 sAssetType = {sAssetType}");
        //            return LoopGenAssetType(sAssetType, lst_GRGAT2, sPrefix, tempItemID, ref iRunning);
        //        }
        //    }

        //    WriteLogFile.LogAddon($"4 sAssetType = {sAssetType} ");
        //    if (lst_GRGAT2.Count > 0)
        //    {
        //        WriteLogFile.LogAddon($"4.1 sAssetType = {sAssetType} ");
        //        iRunning = Convert.ToInt32(lst_GRGAT2.OrderBy(a => a.Value5).Last().Value5.Replace(sPrefix, String.Empty)) + 1;
        //        String sRunning = iRunning.ToString("D4");
        //        sAssetType = $"{sPrefix}{sRunning}";
        //    }

        //    WriteLogFile.LogAddon($"5 sAssetType = {sAssetType}");
        //    return sAssetType;

        //}

        public void Check_IsAssetCode999_And_IsAssetCodeHaveIO(DT_TLI_AAI001_SNDRowCustom apCheck, ref Boolean IsAssetCode999, ref Boolean IsAssetCodeHaveIO)
        {
            String sGDLGRP_AssetCode = apCheck.GDLGRP;

            WriteLogFile.LogAddon($"InsertGRMaster : apCheck.BLART.ToUpper() = {apCheck.BLART.ToUpper()}");
            WriteLogFile.LogAddon($"InsertGRMaster : apCheck.GDLGRP = {apCheck.GDLGRP}");
            if (!String.IsNullOrEmpty(sGDLGRP_AssetCode))
            {
                WriteLogFile.LogAddon($"InsertGRMaster : sGDLGRP_AssetCode.Length = {sGDLGRP_AssetCode.Length}");
                if (sGDLGRP_AssetCode.Length >= 3)
                {
                    String sAssetCode999 = sGDLGRP_AssetCode.Substring(sGDLGRP_AssetCode.Length - 3);

                    WriteLogFile.LogAddon($"InsertGRMaster : sAssetCode999 = {sAssetCode999}");
                    IsAssetCode999 = sAssetCode999 == "999";
                    WriteLogFile.LogAddon($"InsertGRMaster : IsAssetCode999 = {IsAssetCode999}");
                }
            }

            IsAssetCodeHaveIO = !String.IsNullOrEmpty(apCheck.AUFNR);
            WriteLogFile.LogAddon($"apCheck.AUFNR : {apCheck.AUFNR}");
        }

        public void Check_ActiveCheckDEBITCREDIT_And_ActiveNewFormula(TLIContext _context, String sMT_CMAlert_MasterType,
            ref List<MSTMasterData> lst_CMAlert, ref MSTMasterData iGRPOSTSAP, ref MSTMasterData iGRDEBITCREDIT,
            ref Boolean blActiveCheckDEBITCREDIT, ref Boolean blActiveNewFormula)
        {
            lst_CMAlert = _context.MSTMasterDatas.Where(a => a.MasterType == sMT_CMAlert_MasterType && a.IsActive == true).ToList();
            var temp_lst_CMAlert1 = lst_CMAlert.Where(a => a.Value1 == "GRPOSTSAP").ToList();
            var temp_lst_CMAlert2 = lst_CMAlert.Where(a => a.Value1 == "GRDEBITCREDIT").ToList();
            if (temp_lst_CMAlert1.Count > 0)
            {
                iGRPOSTSAP = temp_lst_CMAlert1.First();
            }
            if (temp_lst_CMAlert2.Count > 0)
            {
                iGRDEBITCREDIT = temp_lst_CMAlert2.First();

                List<String> lstValue5 = new List<String>();
                if (!String.IsNullOrEmpty(temp_lst_CMAlert2.First().Value5))
                {
                    lstValue5 = temp_lst_CMAlert2.First().Value5.Split('|').ToList();
                    if (lstValue5.Count >= 1)
                    {
                        blActiveCheckDEBITCREDIT = lstValue5[0] == "T" ? true : false;
                    }
                    if (lstValue5.Count >= 2)
                    {
                        blActiveNewFormula = lstValue5[1] == "T" ? true : false;
                    }
                }
            }
        }

        public String DTO_WRBTR_X(String sWRBTR, String QUNTY)
        {
            decimal iAmount = decimal.Parse(String.IsNullOrEmpty(sWRBTR) ? "0" : sWRBTR) *
                                    decimal.Parse(String.IsNullOrEmpty(QUNTY) ? "0" : QUNTY);
            return iAmount.ToString("#.#0");
        }

        public String DTO_WRBTR_D(String sWRBTR, String QUNTY)
        {
            decimal iAmount = decimal.Parse(String.IsNullOrEmpty(sWRBTR) ? "0" : sWRBTR) /
                                    decimal.Parse(String.IsNullOrEmpty(QUNTY) ? "0" : QUNTY);
            return iAmount.ToString("#.#0");
        }

        [HttpPost]
        [Route("CheckBudget")]
        public async Task<IHttpActionResult> CheckBudget(RequestCheckBudget request)
        {
            var messageValid = AddonManager.CheckRequireBudget(request);
            if (!string.IsNullOrEmpty(messageValid))
            {
                return Ok(new
                {
                    Message = messageValid,
                    MessageType = "e",
                });
            }
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var insufficientItems = new List<object>();

            foreach (var item in request.Orders.GroupBy(g => new { g.BudgetYear, g.Costcenter, g.GLAccount, g.IO })
                .Select(s => new
                {
                    BudgetYear = AddonManager.CleanText(s.Key.BudgetYear),
                    Costcenter = AddonManager.CleanText(s.Key.Costcenter),
                    GLAccount = AddonManager.CleanText(s.Key.GLAccount),
                    IO = AddonManager.CleanText(s.Key.IO ?? ""),
                    Amount = s.Sum(sum => (decimal)double.Parse(sum.Amount))
                }))
            {
                var budgetTracking = await _context.BudgetTrackings
                    .Where(b => b.BudgetCostCenter == item.Costcenter
                                && b.GLAccount == item.GLAccount
                                && b.BudgetYear.ToString() == item.BudgetYear
                                && b.InternalOrder == item.IO)
                    .FirstOrDefaultAsync();

                if (budgetTracking == null)
                {
                    insufficientItems.Add(new
                    {
                        item.BudgetYear,
                        item.Costcenter,
                        item.GLAccount,
                        item.IO,
                        Message = "Not Found Budget",
                        MessageType = "e"
                    });
                }
                else if (budgetTracking.Remaining < item.Amount)
                {
                    insufficientItems.Add(new
                    {
                        item.BudgetYear,
                        item.Costcenter,
                        item.GLAccount,
                        item.IO,
                        budgetTracking.Remaining,
                        Message = "Over budget.",
                        MessageType = "e"
                    });
                }
            }

            if (insufficientItems.Count > 0)
            {
                return Ok(new
                {
                    Message = "Some items have Over budget.",
                    MessageType = "e",
                    InsufficientItems = insufficientItems
                });
            }

            //var transactions = new List<BudgetTransaction>();
            //foreach (var item in request.Orders)
            //{
            //    var transaction = new BudgetTransaction
            //    {
            //        BudgetCostCenter = item.Costcenter,
            //        GLAccount = item.GLAccount,
            //        InternalOrder = item.IO,
            //        TransactionType = BudgetType.Reserve,
            //        Amount = decimal.Parse(item.Amount),
            //        DocumentNo = request.Header.DocumentNo,
            //        Description = "",
            //        BudgetYear = (int)decimal.Parse(item.BudgetYear),
            //        TransactionDate = DateTime.Now
            //    };

            //    transactions.Add(transaction);
            //}

            //_context.BudgetTransactions.AddRange(transactions);
            //await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Budget transactions saved successfully.",
                MessageType = "s"
            });
        }
        [HttpPost]
        [Route("ReserveBudget")]
        public async Task<IHttpActionResult> ReserveBudget(RequestCheckBudget request)
        {
            var messageValid = AddonManager.CheckRequireBudget(request);
            if (!string.IsNullOrEmpty(messageValid))
            {
                return Ok(new
                {
                    Message = messageValid,
                    MessageType = "e",
                });
            }
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var insufficientItems = new List<object>();

            foreach (var item in request.Orders.GroupBy(g => new { g.BudgetYear, g.Costcenter, g.GLAccount, g.IO })
                .Select(s => new
                {
                    BudgetYear = AddonManager.CleanText(s.Key.BudgetYear),
                    Costcenter = AddonManager.CleanText(s.Key.Costcenter),
                    GLAccount = AddonManager.CleanText(s.Key.GLAccount),
                    IO = AddonManager.CleanText(s.Key.IO ?? ""),
                    Amount = s.Sum(sum => (decimal)double.Parse(sum.Amount))
                }))
            {
                var budgetTracking = await _context.BudgetTrackings
                    .Where(b => b.BudgetCostCenter == item.Costcenter
                                && b.GLAccount == item.GLAccount
                                && b.BudgetYear.ToString() == item.BudgetYear
                                && b.InternalOrder == item.IO)
                    .FirstOrDefaultAsync();

                if (budgetTracking == null)
                {
                    insufficientItems.Add(new
                    {
                        item.BudgetYear,
                        item.Costcenter,
                        item.GLAccount,
                        item.IO,
                        Message = "Not Found Budget",
                        MessageType = "e"
                    });
                }
                else if (budgetTracking.Remaining < item.Amount)
                {
                    insufficientItems.Add(new
                    {
                        item.BudgetYear,
                        item.Costcenter,
                        item.GLAccount,
                        item.IO,
                        budgetTracking.Remaining,
                        Message = "Over budget.",
                        MessageType = "e"
                    });
                }
            }

            if (insufficientItems.Count > 0)
            {
                return Ok(new
                {
                    Message = "Some items have Over budget.",
                    MessageType = "e",
                    InsufficientItems = insufficientItems
                });
            }

            var transactions = new List<BudgetTransaction>();
            foreach (var item in request.Orders)
            {
                var transaction = new BudgetTransaction
                {
                    BudgetCostCenter = item.Costcenter,
                    GLAccount = item.GLAccount,
                    InternalOrder = item.IO,
                    TransactionType = BudgetType.Reserve,
                    Amount = decimal.Parse(item.Amount),
                    DocumentNo = request.Header.DocumentNo,
                    Description = "",
                    BudgetYear = (int)decimal.Parse(item.BudgetYear),
                    TransactionDate = DateTime.Now
                };

                transactions.Add(transaction);
            }

            _context.BudgetTransactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Budget transactions saved successfully.",
                MessageType = "s"
            });
        }

        [HttpPost]
        [Route("UsedBudget")]
        public async Task<IHttpActionResult> UsedBudget(RequestCheckBudget request)
        {
            var messageValid = AddonManager.CheckRequireBudget(request);
            if (!string.IsNullOrEmpty(messageValid))
            {
                return Ok(new
                {
                    Message = messageValid,
                    MessageType = "e",
                });
            }
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var transactions = new List<BudgetTransaction>();
            foreach (var item in request.Orders)
            {
                var transaction = new BudgetTransaction
                {
                    BudgetCostCenter = item.Costcenter,
                    GLAccount = item.GLAccount,
                    InternalOrder = item.IO,
                    TransactionType = BudgetType.Use,
                    Amount = decimal.Parse(item.Amount),
                    DocumentNo = request.Header.DocumentNo,
                    Description = "",
                    BudgetYear = (int)decimal.Parse(item.BudgetYear),
                    TransactionDate = DateTime.Now
                };

                transactions.Add(transaction);
            }

            _context.BudgetTransactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Budget transactions saved successfully.",
                MessageType = "s"
            });
        }
        [HttpPost]
        [Route("DiscountUsedBudget")]
        public async Task<IHttpActionResult> DiscountUsedBudget(RequestCheckBudget request)
        {
            var messageValid = AddonManager.CheckRequireBudget(request);
            if (!string.IsNullOrEmpty(messageValid))
            {
                return Ok(new
                {
                    Message = messageValid,
                    MessageType = "e",
                });
            }
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var transactions = new List<BudgetTransaction>();
            foreach (var item in request.Orders)
            {
                var transaction = new BudgetTransaction
                {
                    BudgetCostCenter = item.Costcenter,
                    GLAccount = item.GLAccount,
                    InternalOrder = item.IO,
                    TransactionType = BudgetType.Use,
                    Amount = decimal.Parse(item.Amount),
                    DocumentNo = request.Header.DocumentNo,
                    Description = "",
                    BudgetYear = (int)decimal.Parse(item.BudgetYear),
                    TransactionDate = DateTime.Now
                };

                transactions.Add(transaction);
            }

            _context.BudgetTransactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Budget transactions saved successfully.",
                MessageType = "s"
            });
        }

        [HttpPost]
        [Route("PaidBudget")]
        public async Task<IHttpActionResult> PaidBudget(RequestCheckBudget request)
        {
            var messageValid = AddonManager.CheckRequireBudget(request);
            if (!string.IsNullOrEmpty(messageValid))
            {
                return Ok(new
                {
                    Message = messageValid,
                    MessageType = "e",
                });
            }
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var transactions = new List<BudgetTransaction>();
            foreach (var item in request.Orders)
            {
                var transaction = new BudgetTransaction
                {
                    BudgetCostCenter = item.Costcenter,
                    GLAccount = item.GLAccount,
                    InternalOrder = item.IO,
                    TransactionType = BudgetType.Paid,
                    Amount = decimal.Parse(item.Amount),
                    DocumentNo = request.Header.DocumentNo,
                    Description = "",
                    BudgetYear = (int)decimal.Parse(item.BudgetYear),
                    TransactionDate = DateTime.Now
                };

                transactions.Add(transaction);
            }

            _context.BudgetTransactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Budget transactions saved successfully.",
                MessageType = "s"
            });
        }
        [HttpPost]
        [Route("DiscountPaidBudget")]
        public async Task<IHttpActionResult> DiscountPaidBudget(RequestCheckBudget request)
        {
            var messageValid = AddonManager.CheckRequireBudget(request);
            if (!string.IsNullOrEmpty(messageValid))
            {
                return Ok(new
                {
                    Message = messageValid,
                    MessageType = "e",
                });
            }
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var transactions = new List<BudgetTransaction>();
            foreach (var item in request.Orders)
            {
                var transaction = new BudgetTransaction
                {
                    BudgetCostCenter = item.Costcenter,
                    GLAccount = item.GLAccount,
                    InternalOrder = item.IO,
                    TransactionType = BudgetType.Paid,
                    Amount = decimal.Parse(item.Amount),
                    DocumentNo = request.Header.DocumentNo,
                    Description = "",
                    BudgetYear = (int)decimal.Parse(item.BudgetYear),
                    TransactionDate = DateTime.Now
                };

                transactions.Add(transaction);
            }

            _context.BudgetTransactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Budget transactions saved successfully.",
                MessageType = "s"
            });
        }

        [HttpPost]
        [Route("CancelBudget")]
        public async Task<IHttpActionResult> CancelBudget(RequestCheckBudget request)
        {
            var messageValid = AddonManager.CheckRequireBudget(request);
            if (!string.IsNullOrEmpty(messageValid))
            {
                return Ok(new
                {
                    Message = messageValid,
                    MessageType = "e",
                });
            }
            var transactions = new List<BudgetTransaction>();
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);

            foreach (var item in request.Orders)
            {
                // สร้างรายการการคืนงบ (Release Transaction)
                var releaseTransaction = new BudgetTransaction
                {
                    BudgetCostCenter = item.Costcenter,
                    GLAccount = item.GLAccount,
                    InternalOrder = item.IO,
                    TransactionType = BudgetType.Cancel,
                    Amount = decimal.Parse(item.Amount),
                    DocumentNo = request.Header.DocumentNo,
                    Description = "",
                    BudgetYear = (int)decimal.Parse(item.BudgetYear),
                    TransactionDate = DateTime.Now
                };

                transactions.Add(releaseTransaction);
            }

            // บันทึกการคืนงบลงฐานข้อมูล
            _context.BudgetTransactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Budget transactions saved successfully.",
                MessageType = "s"
            });
        }

        [HttpPost]
        [Route("ReportBudget")]
        public async Task<IHttpActionResult> ReportBudget(RequestReportBudget request)
        {
            //var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            var _context = TLIContext.OpenConnection("data source=DESKTOP-MTFBHTV\\SQLEXPRESS;initial catalog=WolfapproveCore.Dfarm-Dev;persist security info=True;user id=sa;password=pass@word1;");

            _context.Database.CommandTimeout = 300;

            var emps = _context.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee")
                .Where(x => x.Email == request.userPrincipalName || x.Username == request.userPrincipalName)
                .Select(s => new
                {
                    s.DepartmentId,
                    s.EmployeeId,
                }).ToList();

            WriteLogFile.LogAddon($"emps : {emps.Count}", LogModule.RptBudget);
            var empIds = emps.Select(s => s.EmployeeId).ToList();
            var isBudgetTeam = AddonManager.CheckBudgetTeam(request.userPrincipalName);
            WriteLogFile.LogAddon($"isBudgetTeam : {isBudgetTeam}", LogModule.RptBudget);

            var costcenters = new List<string>();
            if (isBudgetTeam == false)
            {
                var depts = emps.Join(_context.MSTDepartments,
                   header => header.DepartmentId,
                   dept => dept.DepartmentId,
                   (h, d) => d.DepartmentCode).ToList();
                WriteLogFile.LogAddon($"depts : {depts.Count}", LogModule.RptBudget);
                costcenters = _context.Database.SqlQuery<Models.CustomEntity.Models.ViewCostcenterOrganizer>("Select * from dbo.ViewCostcenterOrganizer")
                    .Where(x => depts.Contains(x.DepartmentCode) && empIds.Contains(x.EmployeeId))
                    .SelectMany(sm => new[]
                    {
                        sm.DepartmentCode,
                        sm.CenterCCC
                    }).Distinct().ToList();
                WriteLogFile.LogAddon($"costcenters : {costcenters.Count}", LogModule.RptBudget);
                WriteLogFile.LogAddon($"costcenters : {string.Join(",", costcenters)}", LogModule.RptBudget);
            }

            var mstTempCancelControl = _context.MSTMasterDatas.FirstOrDefault(x => x.MasterType == "TEMPCODE_CANCEL");
            var prCancel = mstTempCancelControl.Value1.Split(',').ToList();
            var poCancel = mstTempCancelControl.Value2.Split(',').ToList();
            var grCancel = mstTempCancelControl.Value3.Split(',').ToList();

            var mstTempClosingControl = _context.MSTMasterDatas.FirstOrDefault(x => x.MasterType == "TEMPCODE_CLOSING");
            var prCl = mstTempClosingControl.Value1.Split(',').ToList();
            var poCl = mstTempClosingControl.Value2.Split(',').ToList();

            var masterConfig = await _context.MSTMasterDatas.Where(x => x.MasterType == "BUDGET_CONTROL_TEMPCODE" && x.IsActive == true).FirstOrDefaultAsync();
            if (masterConfig != null)
            {
                var prCode = masterConfig.Value1.Split(',');
                var poCode = masterConfig.Value2.Split(',');
                var grCode = masterConfig.Value3.Split(',');
                WriteLogFile.LogAddon($"START REPORT budgetTrn", LogModule.RptBudget);
                WriteLogFile.LogAddon($"request.Filter.BudgetYear : {request.Filter.BudgetYear}", LogModule.RptBudget);
                var budgetTrn = _context.ViewBudgetTransactions.AsNoTracking().Where(x => 
                        (request.Filter.BudgetYear == 0 || x.BudgetYear == request.Filter.BudgetYear) &&
                        (costcenters.Contains(x.BudgetCostCenter) || isBudgetTeam))
                .Select(item => new ViewBudgetTransactionDto
                {
                    Id = item.Id,
                    BudgetYear = item.BudgetYear,
                    MemoId = item.MemoId,
                    TemplateId = item.TemplateId,
                    DocumentCode = item.DocumentCode,
                    BudgetCostCenter = item.BudgetCostCenter,
                    GLAccount = item.GLAccount,
                    InternalOrder = item.InternalOrder,
                    TransactionType = item.TransactionType,
                    Amount = item.TransactionType == BudgetType.Cancel && (prCl.Contains(item.DocumentCode) || poCl.Contains(item.DocumentCode)) ? -item.Amount : item.Amount,
                    TransactionDate = item.TransactionDate,
                    DocumentNo = item.DocumentNo,
                    Description = item.Description
                }).ToList();
                WriteLogFile.LogAddon($"budgetTrn : {budgetTrn.Count}", LogModule.RptBudget);
                WriteLogFile.LogAddon($"END REPORT budgetTrn", LogModule.RptBudget);

                var memoids = budgetTrn.Select(s => s.MemoId).ToHashSet();
                WriteLogFile.LogAddon($"memoids : {memoids.Count}", LogModule.RptBudget);
                var relate = _context.RelationBudgets.Where(x => memoids.Contains(x.MemoId)).Select(s => new
                {
                    s.MemoId,
                    s.RefByMemoId
                }).ToList();
                WriteLogFile.LogAddon($"relate : {relate.Count}", LogModule.RptBudget);
                memoids.Clear();

                // แปลง prCode, poCode, grCode เป็น HashSet เพื่อการค้นหาที่เร็วขึ้น
                var prCodeSet = new HashSet<string>(prCode);
                var poCodeSet = new HashSet<string>(poCode);
                var grCodeSet = new HashSet<string>(grCode);

                // สร้าง Lookup ล่วงหน้าเพื่อให้เข้าถึง relate ได้เร็ว
                var relateLookup = relate.ToLookup(r => r.MemoId, r => r.RefByMemoId);
                WriteLogFile.LogAddon($"relateLookup", LogModule.RptBudget);
                relate.Clear();

                WriteLogFile.LogAddon($"START REPORT serializeTrn", LogModule.RptBudget);
                // กรองข้อมูลที่จำเป็นล่วงหน้า
                var validPRs = budgetTrn
                    .FindAll(x => prCodeSet.Contains(x.DocumentCode) && x.TransactionType != BudgetType.Cancel);

                var cancelTransactions = budgetTrn
                    .FindAll(x => x.TransactionType == BudgetType.Cancel);

                var groupedPRs = validPRs
                    .GroupBy(pr => new { pr.DocumentNo, pr.BudgetCostCenter, pr.GLAccount, pr.InternalOrder })
                    .ToList();

                var selrialize = groupedPRs
                .Select(s =>
                {
                    var memoId = s.First().MemoId;
                    var relatedPRClosing = cancelTransactions
                    .Where(x =>
                        (relateLookup[memoId].Contains(x.MemoId) || s.Key.DocumentNo == x.DocumentNo) && prCl.Contains(x.DocumentCode) &&
                        s.Key.BudgetCostCenter == x.BudgetCostCenter &&
                        s.Key.GLAccount == x.GLAccount &&
                        s.Key.InternalOrder == x.InternalOrder)
                    .Select(cancelPR =>
                    {
                        return new BudgetReportItem
                        {
                            BudgetCostCenter = cancelPR.BudgetCostCenter,
                            GLAccount = cancelPR.GLAccount,
                            InternalOrder = cancelPR.InternalOrder,
                            MemoId = memoId,
                            DocumentNo = cancelPR.DocumentNo,
                            Type = "PO",
                            Desc = cancelPR.DocumentNo,
                            Summary_Alloc = 0,
                            Summary_Reserve = cancelPR.Amount,
                            Summary_Used = 0,
                            Summary_Paid = 0,
                            Summary_Remaining = 0,
                        };
                    }).ToList();

                    var relatedCancelAmount = cancelTransactions
                    .Where(x =>
                        (relateLookup[memoId].Contains(x.MemoId) || s.Key.DocumentNo == x.DocumentNo) && !prCl.Contains(x.DocumentCode) &&
                        s.Key.BudgetCostCenter == x.BudgetCostCenter &&
                        s.Key.GLAccount == x.GLAccount &&
                        s.Key.InternalOrder == x.InternalOrder)
                    .Sum(x => x.Amount);

                    var childrenPO = budgetTrn
                    .Where(x => relateLookup[memoId].Contains(x.MemoId) && poCodeSet.Contains(x.DocumentCode) && x.TransactionType != BudgetType.Cancel)
                    .GroupBy(po => new { po.DocumentNo, po.BudgetCostCenter, po.GLAccount, po.InternalOrder })
                    .Select(ss =>
                    {
                        var poMemoId = ss.First().MemoId;
                        var relatedPOClosing = cancelTransactions
                        .Where(x =>
                            (relateLookup[poMemoId].Contains(x.MemoId) || ss.Key.DocumentNo == x.DocumentNo) && poCl.Contains(x.DocumentCode) &&
                            s.Key.BudgetCostCenter == x.BudgetCostCenter &&
                            s.Key.GLAccount == x.GLAccount &&
                            s.Key.InternalOrder == x.InternalOrder)
                        .Select(cancelPR =>
                        {
                            return new BudgetReportItem
                            {
                                MemoId = memoId,
                                DocumentNo = cancelPR.DocumentNo,
                                Type = "GR",
                                Desc = cancelPR.DocumentNo,
                                Summary_Alloc = 0,
                                Summary_Reserve = 0,
                                Summary_Used = cancelPR.Amount,
                                Summary_Paid = 0,
                                Summary_Remaining = 0,
                            };
                        }).ToList();

                        var poCancelAmount = cancelTransactions
                        .Where(x =>
                            (relateLookup[poMemoId].Contains(x.MemoId) || ss.Key.DocumentNo == x.DocumentNo) && !poCl.Contains(x.DocumentCode) &&
                            s.Key.BudgetCostCenter == x.BudgetCostCenter &&
                            s.Key.GLAccount == x.GLAccount &&
                            s.Key.InternalOrder == x.InternalOrder)
                        .Sum(x => x.Amount);

                        var childrenGR = budgetTrn
                        .Where(x => relateLookup[poMemoId].Contains(x.MemoId) && grCodeSet.Contains(x.DocumentCode) && x.TransactionType != BudgetType.Cancel)
                        .GroupBy(gr => new { gr.DocumentNo, gr.BudgetCostCenter, gr.GLAccount, gr.InternalOrder })
                        .Select(sss =>
                        {
                            var grMemoId = sss.First().MemoId;
                            var grCancelAmount = cancelTransactions
                                .Where(x =>
                                    (relateLookup[grMemoId].Contains(x.MemoId) || sss.Key.DocumentNo == x.DocumentNo) &&
                                    s.Key.BudgetCostCenter == x.BudgetCostCenter &&
                                    s.Key.GLAccount == x.GLAccount &&
                                    s.Key.InternalOrder == x.InternalOrder)
                                .Sum(x => x.Amount);

                            return new BudgetReportItem
                            {
                                MemoId = grMemoId,
                                Key = sss.Key.DocumentNo,
                                Type = "GR",
                                Desc = sss.Key.DocumentNo,
                                Summary_Alloc = 0,
                                Summary_Reserve = 0,
                                Summary_Used = 0,
                                Summary_Paid = sss.Sum(x => x.Amount) - grCancelAmount,
                                Summary_Remaining = 0
                            };
                        }).Union(relatedPOClosing).ToList();

                        return new BudgetReportItem
                        {
                            MemoId = poMemoId,
                            Key = ss.Key.DocumentNo,
                            Type = "PO",
                            Desc = ss.Key.DocumentNo,
                            Summary_Alloc = 0,
                            Summary_Reserve = 0,
                            Summary_Used = ss.Sum(x => x.Amount) - poCancelAmount,
                            Summary_Paid = 0,
                            Summary_Remaining = 0,
                            Children = childrenGR
                        };
                    }).Union(relatedPRClosing).ToList();

                    return new BudgetReportItem
                    {
                        BudgetCostCenter = s.Key.BudgetCostCenter,
                        GLAccount = s.Key.GLAccount,
                        InternalOrder = s.Key.InternalOrder,
                        MemoId = memoId,
                        DocumentNo = s.Key.DocumentNo,
                        Type = "PR",
                        Desc = s.Key.DocumentNo,
                        Summary_Alloc = 0,
                        Summary_Reserve = s.Sum(x => x.Amount) - relatedCancelAmount,
                        Summary_Used = 0,
                        Summary_Paid = 0,
                        Summary_Remaining = 0,
                        Sum_Reserve = s.Sum(x => x.Amount) - relatedCancelAmount + childrenPO.Sum(po => po.Summary_Reserve),
                        Sum_Used = childrenPO.Sum(po => po.Summary_Used + po.Children.Sum(gr => gr.Summary_Used)),
                        Sum_Paid = childrenPO.Sum(po => po.Summary_Paid + po.Children.Sum(gr => gr.Summary_Paid)),
                        Children = childrenPO
                    };
                }).ToList();

                budgetTrn.Clear();
                WriteLogFile.LogAddon($"END REPORT serializeTrn", LogModule.RptBudget);

                WriteLogFile.LogAddon($"START REPORT dataQuery", LogModule.RptBudget);

                var glReport = _context.BudgetTrackingGLSummaries
                        .Where(x =>
                        (request.Filter.BudgetYear == 0 || x.BudgetYear == request.Filter.BudgetYear) &&
                        (costcenters.Contains(x.BudgetCostCenter) || isBudgetTeam))
                        .Select(s => new BudgetTrackingGLSummaryDto
                        {
                            Id = s.Id,
                            BudgetYear = s.BudgetYear,
                            BudgetCostCenter = s.BudgetCostCenter,
                            BudgetCostCenterName = s.BudgetCostCenterName,
                            Cancelled = s.Cancelled,
                            GLAccount = s.GLAccount,
                            GLAccountName = s.GLAccountName,
                            Allocate = s.Allocate,
                            Paid = s.Paid,
                            Remaining = s.Remaining,
                            Reserved = s.Reserved,
                            Used = s.Used,
                        }).ToList();

                var ioReport = _context.BudgetTrackingIOSummaries
                        .Where(x =>
                        (request.Filter.BudgetYear == 0 || x.BudgetYear == request.Filter.BudgetYear) &&
                        (costcenters.Contains(x.BudgetCostCenter) || isBudgetTeam))
                        .Select(s => new BudgetTrackingIOSummaryDto
                        {
                            Id = s.Id,
                            BudgetYear = s.BudgetYear,
                            BudgetCostCenter = s.BudgetCostCenter,
                            BudgetCostCenterName = s.BudgetCostCenterName,
                            InternalOrder = s.InternalOrder,
                            InternalOrderName = s.InternalOrderName,
                            Cancelled = s.Cancelled,
                            GLAccount = s.GLAccount,
                            GLAccountName = s.GLAccountName,
                            Allocate = s.Allocate,
                            Paid = s.Paid,
                            Remaining = s.Remaining,
                            Reserved = s.Reserved,
                            Used = s.Used,
                        }).ToList();

                var dataQuery = new List<Cost>();

                var costCenters = _context.BudgetTrackingSummaries
                    .Where(x =>
                        (request.Filter.BudgetYear == 0 || x.BudgetYear == request.Filter.BudgetYear) &&
                        (costcenters.Contains(x.BudgetCostCenter) || isBudgetTeam)).ToList();
                WriteLogFile.LogAddon($"costCenters : {costCenters.Count}", LogModule.RptBudget);
                foreach (var cost in costCenters)
                {
                    var glSummaries = glReport
                        .Where(x => (request.Filter.BudgetYear == 0 || x.BudgetYear == request.Filter.BudgetYear) && (x.BudgetCostCenterName == cost.BudgetCostCenterName))
                        .ToList();

                    var glChildren = new List<GL>();

                    foreach (var gl in glSummaries)
                    {
                        var ioSummaries = ioReport
                            .Where(x => (request.Filter.BudgetYear == 0 || x.BudgetYear == request.Filter.BudgetYear) && (x.BudgetCostCenterName == gl.BudgetCostCenterName && x.GLAccountName == gl.GLAccountName))
                            .ToList();

                        var ioChildren = new List<IO>();

                        foreach (var io in ioSummaries)
                        {
                            ioChildren.Add(new IO
                            {
                                BudgetYear = io.BudgetYear,
                                BudgetCostCenter = io.BudgetCostCenter,
                                GLAccount = io.GLAccount,
                                InternalOrder = io.InternalOrder,
                                Key = io.InternalOrderName,
                                Type = "IO",
                                Desc = io.InternalOrderName,
                                Summary_Alloc = io.Allocate,
                                Summary_Reserve = io.Reserved,
                                Summary_Used = io.Used,
                                Summary_Paid = io.Paid,
                                Summary_Cancelled = io.Cancelled,
                                Summary_Remaining = io.Remaining
                            });
                        }

                        glChildren.Add(new GL
                        {
                            BudgetYear = gl.BudgetYear,
                            BudgetCostCenter = gl.BudgetCostCenter,
                            GLAccount = gl.GLAccount,
                            Key = gl.GLAccountName,
                            Type = "GL",
                            Desc = gl.GLAccountName,
                            Summary_Alloc = gl.Allocate,
                            Summary_Reserve = gl.Reserved,
                            Summary_Used = gl.Used,
                            Summary_Paid = gl.Paid,
                            Summary_Cancelled = gl.Cancelled,
                            Summary_Remaining = gl.Remaining,
                            Children = ioChildren
                        });
                    }

                    dataQuery.Add(new Cost
                    {
                        BudgetYear = cost.BudgetYear.Value,
                        BudgetCostCenter = cost.BudgetCostCenter,
                        Key = cost.BudgetCostCenterName,
                        Type = "COST",
                        Desc = cost.BudgetCostCenterName,
                        Summary_Alloc = cost.Allocate,
                        Summary_Reserve = cost.Reserved,
                        Summary_Used = cost.Used,
                        Summary_Paid = cost.Paid,
                        Summary_Cancelled = cost.Cancelled,
                        Summary_Remaining = cost.Remaining,
                        Children = glChildren
                    });
                }
                WriteLogFile.LogAddon($"END REPORT dataQuery", LogModule.RptBudget);

                WriteLogFile.LogAddon($"START REPORT dataReport", LogModule.RptBudget);
                var dataReport = dataQuery
                .Select(group =>
                {
                    var childrenCost = group.Children
                    .Select(glGroup =>
                    {
                        if (glGroup.Children.Any())
                        {
                            // มี Children: สร้าง childrenTrn จาก ioGroup
                            var childrenTrnGL = glGroup.Children
                            .Where(ioGroup => ioGroup.Key != " - ")
                            .Select(ioGroup =>
                            {
                                return new BudgetReportItem
                                {
                                    Key = ioGroup.Key,
                                    Type = ioGroup.Type,
                                    Desc = ioGroup.Desc,
                                    Summary_Alloc = ioGroup.Summary_Alloc,
                                    Summary_Reserve = selrialize.Where(x =>
                                        x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                        x.GLAccount == ioGroup.GLAccount &&
                                        x.InternalOrder == ioGroup.InternalOrder).Sum(s => s.Sum_Reserve),
                                    Summary_Used = selrialize.Where(x =>
                                        x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                        x.GLAccount == ioGroup.GLAccount &&
                                        x.InternalOrder == ioGroup.InternalOrder).Sum(s => s.Sum_Used),
                                    Summary_Paid = selrialize.Where(x =>
                                        x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                        x.GLAccount == ioGroup.GLAccount &&
                                        x.InternalOrder == ioGroup.InternalOrder).Sum(s => s.Sum_Paid),
                                    Summary_Cancelled = ioGroup.Summary_Cancelled,
                                    Summary_Remaining = ioGroup.Summary_Alloc - selrialize.Where(x =>
                                        x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                        x.GLAccount == ioGroup.GLAccount &&
                                        x.InternalOrder == ioGroup.InternalOrder).Sum(s => s.Sum_Reserve),
                                    Sum_Reserve = selrialize.Where(x =>
                                        x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                        x.GLAccount == ioGroup.GLAccount &&
                                        x.InternalOrder == ioGroup.InternalOrder).Sum(s => s.Sum_Reserve),
                                    Sum_Used = selrialize.Where(x =>
                                        x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                        x.GLAccount == ioGroup.GLAccount &&
                                        x.InternalOrder == ioGroup.InternalOrder).Sum(s => s.Sum_Used),
                                    Sum_Paid = selrialize.Where(x =>
                                        x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                        x.GLAccount == ioGroup.GLAccount &&
                                        x.InternalOrder == ioGroup.InternalOrder).Sum(s => s.Sum_Paid),
                                    Children = selrialize
                                        .Where(x =>
                                            x.BudgetCostCenter == ioGroup.BudgetCostCenter &&
                                            x.GLAccount == ioGroup.GLAccount &&
                                            x.InternalOrder == ioGroup.InternalOrder)
                                        .OrderBy(o => o.Key)
                                        .ToList()
                                };
                            })
                            .Union(selrialize.Where(x =>
                                x.BudgetCostCenter == glGroup.BudgetCostCenter &&
                                x.GLAccount == glGroup.GLAccount &&
                                x.InternalOrder == " - "))
                            .OrderBy(o => o.Key)
                            .ToList();

                            return new BudgetReportItem
                            {
                                Key = glGroup.Key,
                                Type = glGroup.Type,
                                Desc = glGroup.Desc,
                                Summary_Alloc = glGroup.Summary_Alloc,
                                Summary_Reserve = childrenTrnGL.Sum(s => s.Sum_Reserve),
                                Summary_Used = childrenTrnGL.Sum(s => s.Sum_Used),
                                Summary_Paid = childrenTrnGL.Sum(s => s.Sum_Paid),
                                Summary_Cancelled = glGroup.Summary_Cancelled,
                                Summary_Remaining = glGroup.Summary_Alloc - childrenTrnGL.Sum(s => s.Sum_Reserve),
                                Children = childrenTrnGL
                            };
                        }
                        else
                        {
                            // ไม่มี Children: ดึงข้อมูลตรงจาก selrialize
                            return new BudgetReportItem
                            {
                                Key = glGroup.Key,
                                Type = glGroup.Type,
                                Desc = glGroup.Desc,
                                Summary_Alloc = glGroup.Summary_Alloc,
                                Summary_Reserve = selrialize.Where(x =>
                                    x.BudgetCostCenter == glGroup.BudgetCostCenter &&
                                    x.GLAccount == glGroup.GLAccount).Sum(s => s.Sum_Reserve),
                                Summary_Used = selrialize.Where(x =>
                                    x.BudgetCostCenter == glGroup.BudgetCostCenter &&
                                    x.GLAccount == glGroup.GLAccount).Sum(s => s.Sum_Used),
                                Summary_Paid = selrialize.Where(x =>
                                    x.BudgetCostCenter == glGroup.BudgetCostCenter &&
                                    x.GLAccount == glGroup.GLAccount).Sum(s => s.Sum_Paid),
                                Summary_Cancelled = glGroup.Summary_Cancelled,
                                Summary_Remaining = glGroup.Summary_Alloc - selrialize.Where(x =>
                                    x.BudgetCostCenter == glGroup.BudgetCostCenter &&
                                    x.GLAccount == glGroup.GLAccount).Sum(s => s.Sum_Reserve),
                                Children = selrialize
                                    .Where(x =>
                                        x.BudgetCostCenter == glGroup.BudgetCostCenter &&
                                        x.GLAccount == glGroup.GLAccount)
                                    .OrderBy(o => o.Key)
                                    .ToList()
                            };
                        }
                    })
                    .OrderBy(o => o.Key)
                    .ToList();
                    WriteLogFile.LogAddon($"childrenCost : {childrenCost.Count}", LogModule.RptBudget);
                    return new BudgetReportItem
                    {
                        Key = group.Key,
                        Type = group.Type,
                        Desc = group.Desc,
                        Summary_Alloc = group.Summary_Alloc,
                        Summary_Reserve = childrenCost.Sum(s => s.Summary_Reserve),
                        Summary_Used = childrenCost.Sum(s => s.Summary_Used),
                        Summary_Paid = childrenCost.Sum(s => s.Summary_Paid),
                        Summary_Cancelled = group.Summary_Cancelled,
                        Summary_Remaining = group.Summary_Remaining,
                        Children = childrenCost
                    };
                })
                .OrderBy(o => o.Key)
                .ToList();

                WriteLogFile.LogAddon($"END REPORT dataReport : {dataReport.Count}", LogModule.RptBudget);

                dataQuery.Clear();
                selrialize.Clear();

                BudgetReportItem CreateBudgetReportItem(string input)
                {
                    var group = JsonConvert.DeserializeObject<BudgetReportItem>(input);

                    group.Children.RemoveAll(x => x.Summary_Reserve == 0 && x.Type == "PR");

                    group.Children.RemoveAll(x => x.Summary_Used == 0 && x.Type == "PO" && !prCl.Any(a => x.Desc.Contains(a)));

                    group.Children.RemoveAll(x => x.Summary_Paid == 0 && x.Type == "GR" && !poCl.Any(a => x.Desc.Contains(a)));

                    return new BudgetReportItem
                    {
                        BudgetCostCenter = group.BudgetCostCenter,
                        GLAccount = group.GLAccount,
                        InternalOrder = group.InternalOrder,
                        MemoId = group.MemoId,
                        Key = Guid.NewGuid().ToString().Replace("-", ""),
                        Type = group.Type,
                        Desc = group.Desc,
                        Summary_Alloc = group.Summary_Alloc,
                        Summary_Reserve = group.Summary_Reserve,
                        Summary_Used = group.Summary_Used,
                        Summary_Paid = group.Summary_Paid,
                        Summary_Remaining = group.Summary_Remaining,
                        Summary_Cancelled = group.Summary_Cancelled,
                        Sum_Reserve = group.Sum_Reserve,
                        Sum_Used = group.Sum_Used,
                        Sum_Paid = group.Sum_Paid,
                        Children = group.Children?
                        .Select(s =>
                        {
                            return CreateBudgetReportItem(s.ToJson());
                        }).ToList() ?? null,
                    };
                }

                var serializeModel = new ReportReposne
                {
                    TotalCount = dataReport.Count(),
                    ReportData = dataReport.Select(s =>
                    {
                        return CreateBudgetReportItem(s.ToJson());
                    }).ToList()
                };
                dataReport.Clear();
                WriteLogFile.LogAddon($"DATA REPORT serializeModel : {serializeModel.ReportData.Count()}", LogModule.RptBudget);

                int total = serializeModel.ReportData.Count();

                serializeModel.TotalCount = total;

                WriteLogFile.LogAddon($"DATA REPORT BUDGET : {dataQuery.Count()}", LogModule.RptBudget);
                return Ok(serializeModel);
            }

            return Ok();
        }

        [HttpGet]
        [Route("GetOptionReport")]
        public IHttpActionResult GetOptionReport()
        {
            var _context = TLIContext.OpenConnection("data source=DESKTOP-MTFBHTV\\SQLEXPRESS;initial catalog=WolfapproveCore.Dfarm-Dev;persist security info=True;user id=sa;password=pass@word1;");

            //var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);

            try
            {
                var data = _context.BudgetTrackingSummaries
                .GroupJoin(_context.ZSYNCBUDGETs,
                bg => new { bg.BudgetCostCenter, bg.BudgetYear},
                initial => new { initial.BudgetCostCenter, initial.BudgetYear },
                (bg, initialGroup) => new { bg, initialGroup })
                .SelectMany(
                x => x.initialGroup,
                (x, initial) => new
                {
                    x.bg.BudgetYear
                }).Distinct().ToList();
                var aaa = _context.ZSYNCBUDGETs.ToList();

                var optionFilter = new
                {
                    Year = data.Where(s => s?.BudgetYear != null)
                    .Select(s => new
                    {
                        Label = s.BudgetYear.ToString(),
                        Value = s.BudgetYear.ToString()
                    }).Distinct().OrderBy(o => o.Label).ToList(),

                    CostCenter = _context.ZSYNCBUDGETs.ToList().Select(s => new
                    {
                        Label = s.BudgetCostCenterName,
                        Value = s.BudgetCostCenterName
                    }).Distinct().OrderBy(o => o.Label).ToList(),

                    GLAccount = _context.ZSYNCBUDGETs.ToList().Select(ss => new
                    {
                        Label = ss.GLAccount,
                        Value = ss.GLAccount
                    }).Distinct().OrderBy(o => o.Label).ToList(),

                    InternalOrder = _context.ZSYNCBUDGETs.ToList().Select(sss => new
                    {
                        Label = sss.InternalOrder + " - " /*+ sss.Name*/,
                        Value = sss.InternalOrder
                    }).Distinct().OrderBy(o => o.Label).ToList()
                };

                return Ok(optionFilter);
            }
            catch (Exception ex)
            {
                WriteLogFile.LogAddon($"{ex.ToString()}", LogModule.RptBudget);

                return InternalServerError(ex);
            }
            finally
            {
                _context.Dispose();
            }
        }

        [HttpPost]
        [Route("GetBudgetTrackingIOSummaries")]
        public async Task<IHttpActionResult> GetBudgetTrackingIOSummaries(int BudgetYear, string userPrincipalName)
        {
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            //var _context = TLIContext.OpenConnection("data source=172.22.10.148;initial catalog=WolfApproveCore.thailife;persist security info=True;user id=wolf-admin;password=UZPj&9e7Hh2)hH98I;");

            var result = new List<BudgetTrackingDto>();
            try
            {
                var emps = _context.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee")
                .Where(x => x.Email == userPrincipalName || x.Username == userPrincipalName)
                .Select(s => new
                {
                    s.DepartmentId,
                    s.EmployeeId,
                }).ToList();

                var empIds = emps.Select(s => s.EmployeeId).ToList();
                var isBudgetTeam = AddonManager.CheckBudgetTeam(userPrincipalName);
                var costcenters = new List<string>();
                if (isBudgetTeam == false)
                {
                    var depts = emps.Join(_context.MSTDepartments,
                       header => header.DepartmentId,
                       dept => dept.DepartmentId,
                       (h, d) => d.DepartmentCode).ToList();

                    costcenters = _context.Database.SqlQuery<Models.CustomEntity.Models.ViewCostcenterOrganizer>("Select * from dbo.ViewCostcenterOrganizer")
                        .Where(x => depts.Contains(x.DepartmentCode) && empIds.Contains(x.EmployeeId))
                        .SelectMany(sm => new[]
                        {
                        sm.DepartmentCode,
                        sm.CenterCCC
                        }).Distinct().ToList();
                    WriteLogFile.LogAddon($"GetBudgetTrackingIOSummaries costcenters : {string.Join(",", costcenters)}", LogModule.RptBudget);
                }

                return Ok(new
                {
                    Message = "Get data BudgetTrackingIOSummaries successfully.",
                    MessageType = "S",
                    Data = _context.BudgetTrackings
                         .Where(x =>
                         (BudgetYear == 0 || x.BudgetYear == BudgetYear) &&
                         (costcenters.Contains(x.BudgetCostCenter) || isBudgetTeam)).ToList()
                         .Select(s => new
                         {
                             s.BudgetYear,
                             s.BudgetCostCenter,
                             BudgetCostCenterName = s.BudgetCostCenterName.Replace($"{s.BudgetCostCenter} - ", ""),
                             s.GLAccount,
                             GLAccountName = s.GLAccountName.Replace($"{s.GLAccount} - ", ""),
                             s.InternalOrder,
                             InternalOrderName = s.InternalOrderName.Replace($"{s.InternalOrder} - ", ""),
                             s.Allocate,
                             s.Reserved,
                             s.Used,
                             s.Paid,
                             s.Remaining,
                         }).OrderBy(o => o.BudgetCostCenterName).ToList(),
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Message = $"Error Exception {ex}",
                    MessageType = "E"
                });
            }
            finally
            {
                _context.Dispose();
            }
        }

        [HttpPost]
        [Route("GetCostcenter")]
        public IHttpActionResult GetCostcenter(CustomClass req)
        {
            try
            {
                var _context = TLIContext.OpenConnection(req.connectionString);
                var lang = _context.MSTEmployees.Where(x => x.Email == req.userPrincipalName).Select(s => s.Lang).FirstOrDefault();

                var data = _context.ZSYNCCCs.Join(
                    _context.MSTEmployees,
                    c => c.CreatedBy,
                    createdBy => createdBy.EmployeeId,
                    (c, createdBy) => new { c, createdBy })
                .Join(
                    _context.MSTEmployees,
                    cWithCreatedBy => cWithCreatedBy.c.ModifiedBy,
                    modifiedBy => modifiedBy.EmployeeId,
                    (cWithCreatedBy, modifiedBy) => new
                    {
                        cWithCreatedBy.c.Id,
                        cWithCreatedBy.c.CostCenterCode,
                        cWithCreatedBy.c.Description,
                        cWithCreatedBy.c.IsActive,
                        cWithCreatedBy.c.CreatedDate,
                        CreatedBy = lang == "TH" ? cWithCreatedBy.createdBy.NameTh : cWithCreatedBy.createdBy.NameEn,
                        cWithCreatedBy.c.ModifiedDate,
                        ModifiedBy = lang == "TH" ? modifiedBy.NameTh : modifiedBy.NameEn,
                    }).ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        [HttpPost]
        [Route("SaveCostcenter")]
        public async Task<IHttpActionResult> SaveCostcenter(RequestZSYNCCCSave requestZSYNCCC)
        {
            try
            {
                using (var _context = TLIContext.OpenConnection(requestZSYNCCC.connectionString))
                {
                    // Get the employee information
                    var emp = _context.MSTEmployees.FirstOrDefault(x => x.Email == requestZSYNCCC.userPrincipalName);
                    if (emp == null) return Ok(false);

                    // Validate the cost center data
                    var newCCs = requestZSYNCCC.zSYNCCCs;
                    if (newCCs != null && newCCs.Any())
                    {
                        foreach (var costcenter in newCCs)
                        {
                            var existingCC = _context.ZSYNCCCs.FirstOrDefault(x => x.CostCenterCode == costcenter.CostCenterCode);

                            if (existingCC != null)
                            {
                                // Update existing record
                                existingCC.ModifiedDate = DateTime.Now;
                                existingCC.ModifiedBy = (int)emp.EmployeeId;
                                existingCC.CostCenterCode = costcenter.CostCenterCode;
                                existingCC.Description = costcenter.Description;
                                existingCC.IsActive = costcenter.IsActive;
                            }
                            else
                            {
                                // Add new record
                                _context.ZSYNCCCs.Add(new ZSYNCCC
                                {
                                    CreatedDate = DateTime.Now,
                                    ModifiedDate = DateTime.Now,
                                    CostCenterCode = costcenter.CostCenterCode,
                                    CreatedBy = (int)emp.EmployeeId,
                                    Description = costcenter.Description,
                                    IsActive = costcenter.IsActive,
                                    ModifiedBy = (int)emp.EmployeeId
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    return Ok(true);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetGLAccount")]
        public IHttpActionResult GetGLAccount(CustomClass req)
        {
            try
            {
                var _context = TLIContext.OpenConnection(req.connectionString);
                var lang = _context.MSTEmployees.Where(x => x.Email == req.userPrincipalName).Select(s => s.Lang).FirstOrDefault();

                var emp_modified = _context.MSTEmployees.ToList();

                var data = _context.ZSYNCGLs.Join(
                    _context.MSTEmployees,
                    c => c.CreatedBy,
                    createdBy => createdBy.EmployeeId,
                    (c, createdBy) => new { c, createdBy })
                .Join(
                    _context.MSTEmployees,
                    cWithCreatedBy => cWithCreatedBy.c.ModifiedBy,
                    modifiedBy => modifiedBy.EmployeeId,
                    (cWithCreatedBy, modifiedBy) => new
                    {
                        cWithCreatedBy.c.Id,
                        cWithCreatedBy.c.GLAccount,
                        cWithCreatedBy.c.Name,
                        cWithCreatedBy.c.Type,
                        //cWithCreatedBy.c.AccountKOKJ,
                        //cWithCreatedBy.c.PostKey,
                        //cWithCreatedBy.c.ANBWA,
                        //cWithCreatedBy.c.NEWKO,
                        //cWithCreatedBy.c.NASSETS,
                        //cWithCreatedBy.c.ANLKL,
                        //cWithCreatedBy.c.PostKeyAA,
                        //cWithCreatedBy.c.ANBWAAA,
                        //cWithCreatedBy.c.NEWUM,
                        //cWithCreatedBy.c.assetgroup,
                        //cWithCreatedBy.c.CategoryGroupCode,

                        cWithCreatedBy.c.IsActive,
                        cWithCreatedBy.c.CreatedDate,
                        CreatedBy = lang == "TH" ? cWithCreatedBy.createdBy.NameTh : cWithCreatedBy.createdBy.NameEn,
                        cWithCreatedBy.c.ModifiedDate,
                        ModifiedBy = lang == "TH" ? modifiedBy.NameTh : modifiedBy.NameEn,
                    }).ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        [HttpPost]
        [Route("SaveGLAccount")]
        public IHttpActionResult SaveGLAccount(RequestZSYNCGLSave request)
        {
            //try
            //{
            //    var _context = TLIContext.OpenConnection(request.connectionString);
            //    var emp = _context.MSTEmployees.FirstOrDefault(x => x.Email == request.userPrincipalName);
            //    var glAccount = _context.ZSYNCGLs.Select(s => s.GLAccount);
            //    var insert = request.zSYNCGLs.Where(x => !glAccount.Contains(x.GLAccount)).ToList();
            //    var update = request.zSYNCGLs.Where(x => glAccount.Contains(x.GLAccount)).ToList();

            //    insert.ForEach(c =>
            //    {
            //        _context.ZSYNCGLs.Add(new ZSYNCGL
            //        {
            //            GLAccount = c.GLAccount,
            //            Name = c.Name,
            //            Type = c.Type,
            //            AccountKOKJ = c.AccountKOKJ,
            //            PostKey = c.PostKey,
            //            ANBWA = c.ANBWA,
            //            NEWKO = c.NEWKO,
            //            NASSETS = c.NASSETS,
            //            ANLKL = c.ANLKL,
            //            PostKeyAA = c.PostKeyAA,
            //            ANBWAAA = c.ANBWAAA,
            //            NEWUM = c.NEWUM,
            //            assetgroup = c.assetgroup,
            //            CategoryGroupCode = c.CategoryGroupCode,
            //            IsActive = c.IsActive,
            //            CreatedDate = DateTime.Now,
            //            CreatedBy = emp.EmployeeId ?? 0,
            //            ModifiedDate = DateTime.Now,
            //            ModifiedBy = emp.EmployeeId ?? 0,
            //        });
            //    });

            //    var updateIds = update.Select(s => s.GLAccount).ToHashSet();
            //    var updates = _context.ZSYNCGLs.Where(x => updateIds.Contains(x.GLAccount)).ToList();
            //    updates.ForEach(f =>
            //    {
            //        var c = request.zSYNCGLs.FirstOrDefault(x => x.Id == f.Id);
            //        f.GLAccount = c.GLAccount;
            //        f.Name = c.Name;
            //        f.Type = c.Type;
            //        f.AccountKOKJ = c.AccountKOKJ;
            //        f.PostKey = c.PostKey;
            //        f.ANBWA = c.ANBWA;
            //        f.NEWKO = c.NEWKO;
            //        f.NASSETS = c.NASSETS;
            //        f.ANLKL = c.ANLKL;
            //        f.PostKeyAA = c.PostKeyAA;
            //        f.ANBWAAA = c.ANBWAAA;
            //        f.NEWUM = c.NEWUM;
            //        f.assetgroup = c.assetgroup;
            //        f.CategoryGroupCode = c.CategoryGroupCode;
            //        f.IsActive = c.IsActive;
            //        f.ModifiedDate = DateTime.Now;
            //        f.ModifiedBy = emp.EmployeeId ?? 0;
            //    });

            //    _context.SaveChanges();

            //    return Ok(true);
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception(ex.ToString());
            //}

            try
            {
                using (var _context = TLIContext.OpenConnection(request.connectionString))
                {
                    var emp = _context.MSTEmployees.FirstOrDefault(x => x.Email == request.userPrincipalName);
                    if (emp == null) return Ok(false);

                    var newGLs = request.zSYNCGLs;

                    if (newGLs != null && newGLs.Any())
                    {
                        foreach (var gl in newGLs)
                        {
                            var existingGL = _context.ZSYNCGLs.FirstOrDefault(x => x.GLAccount == gl.GLAccount);

                            if (existingGL != null)
                            {
                                existingGL.GLAccount = gl.GLAccount;
                                existingGL.Name = gl.Name;
                                existingGL.Type = gl.Type;
                                //existingGL.AccountKOKJ = gl.AccountKOKJ;
                                //existingGL.PostKey = gl.PostKey;
                                //existingGL.ANBWA = gl.ANBWA;
                                //existingGL.NEWKO = gl.NEWKO;
                                //existingGL.NASSETS = gl.NASSETS;
                                //existingGL.ANLKL = gl.ANLKL;
                                //existingGL.PostKeyAA = gl.PostKeyAA;
                                //existingGL.ANBWAAA = gl.ANBWAAA;
                                //existingGL.NEWUM = gl.NEWUM;
                                //existingGL.assetgroup = gl.assetgroup;
                                //existingGL.CategoryGroupCode = gl.CategoryGroupCode;

                                existingGL.IsActive = gl.IsActive;
                                existingGL.ModifiedDate = DateTime.Now;
                                existingGL.ModifiedBy = emp.EmployeeId ?? 0;
                            }
                            else
                            {
                                _context.ZSYNCGLs.Add(new ZSYNCGL
                                {
                                    GLAccount = gl.GLAccount,
                                    Name = gl.Name,
                                    Type = gl.Type,
                                    //AccountKOKJ = gl.AccountKOKJ,
                                    //PostKey = gl.PostKey,
                                    //ANBWA = gl.ANBWA,
                                    //NEWKO = gl.NEWKO,
                                    //NASSETS = gl.NASSETS,
                                    //ANLKL = gl.ANLKL,
                                    //PostKeyAA = gl.PostKeyAA,
                                    //ANBWAAA = gl.ANBWAAA,
                                    //NEWUM = gl.NEWUM,
                                    //assetgroup = gl.assetgroup,
                                    //CategoryGroupCode = gl.CategoryGroupCode,

                                    IsActive = gl.IsActive,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = emp.EmployeeId ?? 0,
                                    ModifiedDate = DateTime.Now,
                                    ModifiedBy = emp.EmployeeId ?? 0
                                });
                            }
                        }

                        _context.SaveChanges();
                    }

                    return Ok(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        [HttpPost]
        [Route("GetZSYNCIO")]
        public IHttpActionResult GetZSYNCIO(CustomClass req)
        {
            try
            {
                var _context = TLIContext.OpenConnection(req.connectionString);

                var lang = _context.MSTEmployees
                    .Where(x => x.Email == req.userPrincipalName)
                    .Select(s => s.Lang)
                    .FirstOrDefault();

                var data = _context.ZSYNCIOs.Join(
                    _context.MSTEmployees,
                    io => io.CreatedBy,
                    createdBy => createdBy.EmployeeId,
                    (io, createdBy) => new { io, createdBy })
                .Join(
                    _context.MSTEmployees,
                    ioWithCreatedBy => ioWithCreatedBy.io.ModifiedBy,
                    modifiedBy => modifiedBy.EmployeeId,
                    (ioWithCreatedBy, modifiedBy) => new
                    {
                        ioWithCreatedBy.io.Id,
                        ioWithCreatedBy.io.InternalOrder,
                        ioWithCreatedBy.io.Name,
                        ioWithCreatedBy.io.IsActive,
                        ioWithCreatedBy.io.CreatedDate,
                        CreatedBy = lang == "TH" ? ioWithCreatedBy.createdBy.NameTh : ioWithCreatedBy.createdBy.NameEn,
                        ioWithCreatedBy.io.ModifiedDate,
                        ModifiedBy = lang == "TH" ? modifiedBy.NameTh : modifiedBy.NameEn,
                    }).ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        [HttpPost]
        [Route("SaveZSYNCIO")]
        public async Task<IHttpActionResult> SaveZSYNCIO(RequestZSYNCIOSave requestZSYNCIO)
        {
            try
            {
                using (var _context = TLIContext.OpenConnection(requestZSYNCIO.connectionString))
                {
                    var emp = _context.MSTEmployees.FirstOrDefault(x => x.Email == requestZSYNCIO.userPrincipalName);
                    if (emp == null) return Ok(false);

                    var newIOs = requestZSYNCIO.zSYNCIOs;

                    if (newIOs != null && newIOs.Any())
                    {
                        foreach (var io in newIOs)
                        {
                            var existingIO = _context.ZSYNCIOs.FirstOrDefault(x => x.InternalOrder == io.InternalOrder);

                            if (existingIO != null)
                            {
                                existingIO.InternalOrder = io.InternalOrder;
                                existingIO.Name = io.Name;
                                existingIO.IsActive = io.IsActive;
                                existingIO.ModifiedDate = DateTime.Now;
                                existingIO.ModifiedBy = emp.EmployeeId ?? 0;
                            }
                            else
                            {
                                _context.ZSYNCIOs.Add(new ZSYNCIO
                                {
                                    InternalOrder = io.InternalOrder,
                                    Name = io.Name,
                                    IsActive = io.IsActive,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = emp.EmployeeId ?? 0,
                                    ModifiedDate = DateTime.Now,
                                    ModifiedBy = emp.EmployeeId ?? 0
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    return Ok(true);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetMstAsset")]
        public IHttpActionResult GetMstAsset(CustomClass req)
        {
            try
            {
                var _context = TLIContext.OpenConnection(req.connectionString);

                var lang = _context.MSTEmployees
                    .Where(x => x.Email == req.userPrincipalName)
                    .Select(s => s.Lang)
                    .FirstOrDefault();

                var data = _context.MSTAssets.Join(
                    _context.MSTEmployees,
                    asset => asset.CreatedBy,
                    createdBy => createdBy.EmployeeId,
                    (asset, createdBy) => new { asset, createdBy })
                .Join(
                    _context.MSTEmployees,
                    assetWithCreatedBy => assetWithCreatedBy.asset.ModifiedBy,
                    modifiedBy => modifiedBy.EmployeeId,
                    (assetWithCreatedBy, modifiedBy) => new
                    {
                        assetWithCreatedBy.asset.Id,
                        assetWithCreatedBy.asset.GLAccountCode,
                        assetWithCreatedBy.asset.AssetClass,
                        assetWithCreatedBy.asset.CategoryCode,
                        assetWithCreatedBy.asset.AssetCode,
                        assetWithCreatedBy.asset.AssetName,
                        assetWithCreatedBy.asset.IsActive,
                        assetWithCreatedBy.asset.CreatedDate,
                        CreatedBy = lang == "TH" ? assetWithCreatedBy.createdBy.NameTh : assetWithCreatedBy.createdBy.NameEn,
                        assetWithCreatedBy.asset.ModifiedDate,
                        ModifiedBy = lang == "TH" ? modifiedBy.NameTh : modifiedBy.NameEn,
                    }).ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        [HttpPost]
        [Route("SaveMstAsset")]
        public async Task<IHttpActionResult> SaveMstAssetAsync(RequestMSTAsset requestMSTAsset)
        {
            try
            {
                using (var _context = TLIContext.OpenConnection(requestMSTAsset.connectionString))
                {
                    var emp = _context.MSTEmployees.FirstOrDefault(x => x.Email == requestMSTAsset.userPrincipalName);
                    if (emp == null) return Ok(false);

                    var assets = requestMSTAsset.mstAssets;

                    if (assets != null && assets.Any())
                    {
                        foreach (var asset in assets)
                        {
                            var existingAsset = _context.MSTAssets.FirstOrDefault(x =>
                                x.GLAccountCode == asset.GLAccountCode &&
                                x.AssetClass == asset.AssetClass &&
                                x.CategoryCode == asset.CategoryCode &&
                                x.AssetCode == asset.AssetCode
                                );

                            if (existingAsset != null)
                            {
                                existingAsset.GLAccountCode = asset.GLAccountCode;
                                existingAsset.AssetClass = asset.AssetClass;
                                existingAsset.CategoryCode = asset.CategoryCode;
                                existingAsset.AssetCode = asset.AssetCode;
                                existingAsset.AssetName = asset.AssetName;
                                existingAsset.IsActive = asset.IsActive;
                                existingAsset.ModifiedDate = DateTime.Now;
                                existingAsset.ModifiedBy = emp.EmployeeId ?? 0;
                            }
                            else
                            {
                                // Add new record
                                _context.MSTAssets.Add(new MSTAsset
                                {
                                    GLAccountCode = asset.GLAccountCode,
                                    AssetClass = asset.AssetClass,
                                    CategoryCode = asset.CategoryCode,
                                    AssetCode = asset.AssetCode,
                                    AssetName = asset.AssetName,
                                    IsActive = asset.IsActive,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = emp.EmployeeId ?? 0,
                                    ModifiedDate = DateTime.Now,
                                    ModifiedBy = emp.EmployeeId ?? 0
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    return Ok(true);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetPermissionReport")]
        public IHttpActionResult GetPermissionReport(int BudgetYear, string userPrincipalName)
        {
            var _context = TLIContext.OpenConnection(WebConfigurationManager.AppSettings["ConnectionString"]);

            var emps = _context.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee")
                .Where(x => x.Email == userPrincipalName || x.Username == userPrincipalName)
                .Select(s => new
                {
                    s.DepartmentId,
                    s.EmployeeId,
                }).ToList();

            var empIds = emps.Select(s => s.EmployeeId).ToList();
            var isBudgetTeam = (from role in _context.MSTRoles
                                join userPerm in _context.MSTUserPermissions
                                on role.RoleId equals userPerm.RoleId
                                where empIds.Contains(userPerm.EmployeeId ?? 0) && userPerm.IsDelete != true &&
                                      role.IsActive == true
                                select new
                                {
                                    role.NameEn,
                                    role.NameTh,
                                    role.RoleId
                                }).Any(a => a.NameEn == "BUDGET TEAM" || a.NameTh == "BUDGET TEAM");

            return Ok(isBudgetTeam);
        }
    }
}