using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web.Configuration;
using System.Web.UI.WebControls;
using AddonProcurement.Extention;
using AddonProcurement.GRServiceReference;
using AddonProcurement.Helpers;
using AddonProcurement.Models;
using AddonProcurement.Models.CustomEntity;
using AddonProcurement.Models.RequestModel;
using AddonProcurement.Models.ResponeModel;
using AddonProcurement.VendorMasterServiceReference;
using WolfApprove.API2.Extension;
using WolfApprove.Model;
using WolfApprove.Model.CustomClass;
using WolfApprove.Model.Extension;
using static AddonProcurement.Helpers.WriteLogFile;
using AdvanceFormExt = AddonProcurement.Helpers.AdvanceFormExt;
using System.Threading.Tasks;
using WolfApprove.API2.Controllers.Utils;

namespace AddonProcurement.Managers
{
    public class AddonManager
    {
        public static string ConnectionString = WebConfigurationManager.AppSettings["ConnectionString"];
        public static List<int> FindRefAll(int memoId, string connectionString)
        {
            var result = new List<int>();
            var belowIds = GetBelowMemos(memoId, connectionString);
            var aboveIds = GetAboveMemos(memoId, connectionString);
            result.AddRange(belowIds);
            result.AddRange(aboveIds);

            return result;
        }

        private static List<int> GetBelowMemos(int startMemoID, string connectionString)
        {
            var dbContext = TLIContext.OpenConnection(connectionString);
            var result = new List<int>();
            var stack = new Stack<int>();
            stack.Push(startMemoID);

            while (stack.Count > 0)
            {
                int currentMemoID = stack.Pop();
                result.Add(currentMemoID);

                var nextMemos = dbContext.TRNReferenceDocs.Where(m => m.MemoID == currentMemoID).ToList();

                foreach (var next in nextMemos)
                {
                    stack.Push(next.MemoRefDocID.Value);
                }
            }

            return result;
        }

        private static List<int> GetAboveMemos(int startMemoID, string connectionString)
        {
            var dbContext = TLIContext.OpenConnection(connectionString);
            var result = new List<int>();
            var stack = new Stack<int>();
            stack.Push(startMemoID);

            while (stack.Count > 0)
            {
                int currentMemoID = stack.Pop();
                result.Add(currentMemoID);

                var aboveMemos = dbContext.TRNReferenceDocs.Where(m => m.MemoRefDocID == currentMemoID).ToList();

                foreach (var above in aboveMemos)
                {
                    stack.Push(above.MemoID);
                }
            }

            return result;
        }
        public static List<MAdvancveFormResponeModel> MAdvancveFormByMemoIds(RequestMadvanceForm body)
        {
            LogAddon($"Start MAdvancveFormByMemoIds TemplateId : {body.TemplateId} ", LogModule.RefForm);
            var result = new List<MAdvancveFormResponeModel>();
            using (WolfApproveModel db = DBContext.OpenConnection(body.connectionString))
            {
                var Main_template = db.MSTTemplates.Where(x => x.TemplateId == body.TemplateId).FirstOrDefault();
                result = db.TRNMemoes.Where(x => body.MemoIds.Contains(x.MemoId)).Select(x => new MAdvancveFormResponeModel
                {
                    MemoId = x.MemoId,
                    MAdvancveForm = x.MAdvancveForm,
                    DocumentNo = x.DocumentNo,
                    TemplateId = x.TemplateId ?? 0,
                    DocumentCode = db.MSTTemplates.Where(t => t.TemplateId == x.TemplateId).Select(s => s.DocumentCode).FirstOrDefault() ?? ""
                }).ToList();
                LogAddon($"result count : {result.Count}", LogModule.RefForm);

                var mstTempCancelControl = db.MSTMasterDatas.FirstOrDefault(x => x.MasterType == "TEMPCODE_CANCEL");
                var prCancel = mstTempCancelControl.Value1.Split(',').ToList();
                var poCancel = mstTempCancelControl.Value2.Split(',').ToList();
                var grCancel = mstTempCancelControl.Value3.Split(',').ToList();
                var prCancelIds = db.MSTTemplates.Where(x => prCancel.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
                var poCancelIds = db.MSTTemplates.Where(x => poCancel.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
                var grCancelIds = db.MSTTemplates.Where(x => grCancel.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
                List<int?> templateCancel = new List<int?>();
                if (prCancelIds.Any()) templateCancel.AddRange(prCancelIds);
                if (poCancelIds.Any()) templateCancel.AddRange(poCancelIds);
                if (grCancelIds.Any()) templateCancel.AddRange(grCancelIds);


                foreach (var memo in result)
                {
                    try
                    {
                        #region Non-Receive
                        string memotable = string.Empty;
                        string Qty = string.Empty;
                        string Amount = string.Empty;
                        string ReceiveQty = string.Empty;
                        var lstmstSetLabelPurchase = db.MSTMasterDatas.Where(
                            x => x.MasterType == "SET_LABEL_PURCHASE" &&
                            x.IsActive == true &&
                            x.Value4.Contains(Main_template.DocumentCode) &&
                            x.Value3.Contains(memo.DocumentCode) &&
                            x.Value1 == "Non-Receive").FirstOrDefault();
                        if (lstmstSetLabelPurchase != null)
                        {
                            var lstLabelPurchase = lstmstSetLabelPurchase.Value2?.Split('|').ToList();
                            if (lstLabelPurchase != null && lstLabelPurchase.Count == 3)
                            {
                                memotable = !string.IsNullOrEmpty(lstLabelPurchase[0]) ? lstLabelPurchase[0] : string.Empty;  //รายการ
                                Qty = !string.IsNullOrEmpty(lstLabelPurchase[1]) ? lstLabelPurchase[1] : string.Empty;        //จำนวน
                                Amount = !string.IsNullOrEmpty(lstLabelPurchase[2]) ? lstLabelPurchase[2] : string.Empty;     //จำนวนเงิน
                                if (db.TRNMemoes.Any(x => memo.MemoId == x.MemoId))
                                {
                                    var memoRelate = db.TRNReferenceDocs.Where(x => x.MemoRefDocID == memo.MemoId)
                                        .Join(db.TRNMemoes,
                                        rm => rm.MemoID,
                                        m => m.MemoId,
                                        (rm, m) => new MemoDto
                                        {
                                            StatusName = m.StatusName,
                                            MAdvancveForm = m.MAdvancveForm,
                                            MemoId = m.MemoId,
                                            TemplateId = m.TemplateId
                                        }).Where(x => x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled).ToList();

                                    var templateIds = memoRelate.Select(x => x.TemplateId).Distinct().ToList();
                                    var templateMap = db.MSTTemplates
                                        .Where(x => templateIds.Contains(x.TemplateId))
                                        .Select(x => new { x.TemplateId, x.DocumentCode })
                                        .ToDictionary(x => x.TemplateId, x => x.DocumentCode);

                                    foreach (var item in memoRelate)
                                    {
                                        if (item.TemplateId.HasValue && templateMap.ContainsKey(item.TemplateId.Value))
                                        {
                                            item.DocumentCode = templateMap[item.TemplateId.Value];
                                        }
                                    }
                                    memoRelate.RemoveAll(r => !lstmstSetLabelPurchase.Value4.Contains(r.DocumentCode));

                                    List<int?> removeref = new List<int?>();
                                    if (memoRelate.Any())
                                    {
                                        var refMemoIds = memoRelate.SelectMany(itemmm => db.TRNReferenceDocs.Where(x => x.MemoRefDocID == itemmm.MemoId).Select(x => x.MemoID)).ToList();
                                        removeref = db.TRNMemoes.Where(x => refMemoIds.Contains(x.MemoId) && templateCancel.Contains(x.TemplateId) && x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled).Select(x => (int?)x.MemoId).ToList();
                                        if (removeref.Any())
                                        {
                                            var remove = db.TRNReferenceDocs.Where(x => removeref.Contains(x.MemoID)).Select(x => x.MemoRefDocID).ToList();
                                            memoRelate.RemoveAll(r => remove.Contains(r.MemoId));
                                        }
                                    }

                                    LogAddon($"memoRelate count : {memoRelate.Count}", LogModule.RefForm);

                                    var initialRequest = new MemoDto
                                    {
                                        MAdvancveForm = memo.MAdvancveForm,
                                        MemoId = memo.MemoId,
                                        TemplateId = memo.TemplateId
                                    };

                                    var initialMemo = initialRequest;
                                    var memos = memoRelate.FindAll(x => x.MemoId != initialMemo.MemoId); //memos ใบที่กด ref //ใบPO

                                    #region เก็บค่า Amount และ Price ที่มาจากใบที่ถูก ref ในทุก row
                                    var order_List = AdvanceFormExt.GetDataTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                    var id_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == "id")?.Index ?? -1;
                                    var amount_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Qty)?.Index ?? -1;
                                    var price_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;

                                    if (id_Index == -1 || amount_Index == -1) continue;

                                    var initial_Orders = new List<OrderModel>();
                                    foreach (var order in order_List?.row)
                                    {
                                        LogAddon($"initial_Oerder : {order[id_Index].value} : {order[amount_Index].value}", LogModule.RefForm);
                                        initial_Orders.Add(new OrderModel
                                        {
                                            Id = order[id_Index].value,
                                            Amount = double.Parse(order[amount_Index]?.value ?? "0"),
                                            Price = double.Parse(order[price_Index]?.value ?? "0"),
                                        });
                                    }
                                    #endregion

                                    #region เก็บค่า Amount และ Price ที่มาจากใบที่กด ref ในทุก row
                                    var total_Orders = new List<OrderModel>();
                                    bool checkpricrisnull = false;
                                    bool checkamountisnull = false;
                                    foreach (var memoItem in memos)
                                    {
                                        var memoItem_order_List = AdvanceFormExt.GetDataTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                        if (memoItem_order_List == null)
                                        {
                                            continue;
                                        }
                                        var memoItem_id_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == "id")?.Index ?? 0;
                                        var memoItem_amount_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == Qty)?.Index ?? 0;
                                        var memoItem_price_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;

                                        var memoItem_initial_Orders = new List<OrderModel>();
                                        if (memoItem_order_List?.row != null)
                                        {
                                            if (memoItem_id_Index >= 0 && memoItem_amount_Index >= 0 && memoItem_price_Index >= 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = double.Parse(order[memoItem_amount_Index]?.value ?? "0"),
                                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0"),
                                                    });
                                                }
                                            }
                                            else if (memoItem_price_Index < 0 && memoItem_amount_Index < 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    checkpricrisnull = true;
                                                    checkamountisnull = true;
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = 0,
                                                        Price = 0
                                                    });
                                                }
                                            }
                                            else if (memoItem_price_Index > 0 && memoItem_amount_Index < 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    checkamountisnull = true;
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = 0,
                                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0")
                                                    });
                                                }
                                            }
                                            else if (memoItem_amount_Index > 0 && memoItem_price_Index < 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    checkpricrisnull = true;
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = double.Parse(order[memoItem_amount_Index]?.value ?? "0"),
                                                        Price = 0
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    #endregion

                                    #region จัดกลุ่มใบที่กด ref ในทุก row
                                    total_Orders = total_Orders
                                        .GroupBy(x => x.Id)
                                        .Select(g =>
                                        {
                                            var orderId = g.Key;
                                            var extractMode = g.Key.Count() >= 2;
                                            var totalAmount = g.Sum(x => x.Amount);
                                            var totalPrice = g.Sum(x => x.Price);

                                            return new OrderModel
                                            {
                                                Id = orderId,
                                                Amount = totalAmount,
                                                Price = totalPrice,
                                                ExtractMode = extractMode
                                            };
                                        })
                                        .ToList(); //จัดกลุ่มของ total_Orders
                                    #endregion

                                    #region คำนวนโดยใบที่ถูก ref - ใบที่กด ref 
                                    var orderDifferences = total_Orders
                                        .Join(initial_Orders,
                                              total => total.Id,
                                              initial => initial.Id,
                                              (total, initial) =>
                                              {
                                                  var orderId = total.Id;
                                                  var totalAmount = total.Amount;
                                                  var initialAmount = initial.Amount;
                                                  var initialPrice = initial.Price;
                                                  var totalPrice = total.Price;
                                                  double differencePrice = 0;
                                                  double differenceAmount = 0;
                                                  if (!checkpricrisnull)
                                                  {
                                                      differencePrice = initialPrice - totalPrice;
                                                  }
                                                  if (!checkamountisnull)
                                                  {
                                                      differenceAmount = initialAmount - totalAmount;
                                                  }
                                                  return new OrderModel
                                                  {
                                                      Id = orderId,
                                                      Amount = differenceAmount,
                                                      Price = differencePrice
                                                  };
                                              })
                                        .ToList();
                                    #endregion

                                    var removeOders = orderDifferences.FindAll(x => x.Amount <= 0 && x.Price <= 0);
                                    LogAddon($"result removeOders : {removeOders.ToJson()}", LogModule.RefForm);

                                    foreach (var od in orderDifferences)
                                    {
                                        var rowIndex = AdvanceFormExt.FindRowIndexById(order_List, od.Id);
                                        if (rowIndex == -1) continue;
                                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, amount_Index, od.Amount.ToString()); //replace amount ลงในตาราง
                                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, price_Index, od.Price.ToString()); //replace price ลงในตาราง
                                    }

                                    foreach (var od in removeOders)
                                    {
                                        order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                                    }

                                    LogAddon($"result OrderList : {order_List.ToJson()}", LogModule.RefForm);
                                    memo.MAdvancveForm = AdvanceFormExt.ReplaceDataTable(memo.MAdvancveForm, JObject.Parse(JsonConvert.SerializeObject(order_List)), !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                }
                            }
                        }
                        #endregion
                        #region Receive
                        var lstmstSetLabelPurchaseReceive = db.MSTMasterDatas.Where(
                            x => x.MasterType == "SET_LABEL_PURCHASE" &&
                            x.IsActive == true &&
                            x.Value4.Contains(Main_template.DocumentCode) &&
                            x.Value3.Contains(memo.DocumentCode) &&
                            x.Value1 == "Receive").FirstOrDefault();
                        if (lstmstSetLabelPurchaseReceive != null)
                        {
                            var lstLabelPurchase = lstmstSetLabelPurchaseReceive.Value2?.Split('|').ToList();
                            if (lstLabelPurchase != null && lstLabelPurchase.Count == 4)
                            {
                                memotable = !string.IsNullOrEmpty(lstLabelPurchase[0]) ? lstLabelPurchase[0] : string.Empty;  //รายการ
                                Qty = !string.IsNullOrEmpty(lstLabelPurchase[1]) ? lstLabelPurchase[1] : string.Empty;        //จำนวน
                                Amount = !string.IsNullOrEmpty(lstLabelPurchase[2]) ? lstLabelPurchase[2] : string.Empty;     //จำนวนเงิน
                                ReceiveQty = !string.IsNullOrEmpty(lstLabelPurchase[3]) ? lstLabelPurchase[3] : string.Empty;
                                if (db.TRNMemoes.Any(x => memo.MemoId == x.MemoId))
                                {
                                    var memoRelate = db.TRNReferenceDocs.Where(x => x.MemoRefDocID == memo.MemoId)
                                        .Join(db.TRNMemoes,
                                        rm => rm.MemoID,
                                        m => m.MemoId,
                                        (rm, m) => new MemoDto
                                        {
                                            StatusName = m.StatusName,
                                            MAdvancveForm = m.MAdvancveForm,
                                            MemoId = m.MemoId,
                                            TemplateId = m.TemplateId
                                        }).Where(x => x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled).ToList();

                                    var templateIds = memoRelate.Select(x => x.TemplateId).Distinct().ToList();
                                    var templateMap = db.MSTTemplates
                                        .Where(x => templateIds.Contains(x.TemplateId))
                                        .Select(x => new { x.TemplateId, x.DocumentCode })
                                        .ToDictionary(x => x.TemplateId, x => x.DocumentCode);

                                    foreach (var item in memoRelate)
                                    {
                                        if (item.TemplateId.HasValue && templateMap.ContainsKey(item.TemplateId.Value))
                                        {
                                            item.DocumentCode = templateMap[item.TemplateId.Value];
                                        }
                                    }
                                    memoRelate.RemoveAll(r => !lstmstSetLabelPurchaseReceive.Value4.Contains(r.DocumentCode));

                                    List<int?> removeref = new List<int?>();
                                    if (memoRelate.Any())
                                    {
                                        var refMemoIds = memoRelate.SelectMany(itemmm => db.TRNReferenceDocs.Where(x => x.MemoRefDocID == itemmm.MemoId).Select(x => x.MemoID)).ToList();
                                        removeref = db.TRNMemoes.Where(x => refMemoIds.Contains(x.MemoId) && templateCancel.Contains(x.TemplateId) && x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled).Select(x => (int?)x.MemoId).ToList();
                                        if (removeref.Any())
                                        {
                                            var remove = db.TRNReferenceDocs.Where(x => removeref.Contains(x.MemoID)).Select(x => x.MemoRefDocID).ToList();
                                            memoRelate.RemoveAll(r => remove.Contains(r.MemoId));
                                        }
                                    }


                                    LogAddon($"memoRelate count : {memoRelate.Count}", LogModule.RefForm);

                                    var initialRequest = new MemoDto
                                    {
                                        MAdvancveForm = memo.MAdvancveForm,
                                        MemoId = memo.MemoId,
                                        TemplateId = memo.TemplateId
                                    };

                                    var initialMemo = initialRequest;
                                    var memos = memoRelate.FindAll(x => x.MemoId != initialMemo.MemoId); //memos ใบที่กด ref //ใบPO

                                    #region เก็บค่า Amount และ Price ที่มาจากใบที่ถูก ref ในทุก row
                                    var order_List = AdvanceFormExt.GetDataTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                    var id_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == "id")?.Index ?? -1;
                                    var amount_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Qty)?.Index ?? -1;
                                    var price_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;
                                    var ReceiveQty_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == ReceiveQty)?.Index ?? -1;

                                    var initial_Orders = new List<OrderModel>();
                                    foreach (var order in order_List?.row)
                                    {
                                        LogAddon($"initial_Oerder : {order[id_Index].value} : {order[amount_Index].value}", LogModule.RefForm);
                                        initial_Orders.Add(new OrderModel
                                        {
                                            Id = order[id_Index].value,
                                            Amount = double.Parse(order[amount_Index]?.value ?? "0"),
                                            Price = double.Parse(order[price_Index]?.value ?? "0"),
                                        });
                                    }
                                    #endregion

                                    #region เก็บค่า Amount และ Price ที่มาจากใบที่กด ref ในทุก row
                                    var total_Orders = new List<OrderModel>();
                                    bool checkpricrisnull = false;
                                    bool checkamountisnull = false;
                                    foreach (var memoItem in memos)
                                    {
                                        var memoItem_order_List = AdvanceFormExt.GetDataTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                        if (memoItem_order_List == null)
                                        {
                                            continue;
                                        }
                                        var memoItem_id_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == "id")?.Index ?? 0;
                                        var memoItem_amount_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == Qty)?.Index ?? 0;
                                        var memoItem_price_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;
                                        var memoItem__ReceiveQty_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == ReceiveQty)?.Index ?? -1;
                                        var memoItem_initial_Orders = new List<OrderModel>();
                                        if (memoItem_order_List?.row != null)
                                        {
                                            if (memoItem_id_Index >= 0 && memoItem__ReceiveQty_Index >= 0 && memoItem_price_Index >= 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = double.Parse(order[memoItem__ReceiveQty_Index]?.value ?? "0"),
                                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0"),
                                                    });
                                                }
                                            }
                                            else if (memoItem_price_Index < 0 && memoItem__ReceiveQty_Index < 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    checkpricrisnull = true;
                                                    checkamountisnull = true;
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = 0,
                                                        Price = 0
                                                    });
                                                }
                                            }
                                            else if (memoItem_price_Index > 0 && memoItem__ReceiveQty_Index < 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    checkamountisnull = true;
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = 0,
                                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0")
                                                    });
                                                }
                                            }
                                            else if (memoItem__ReceiveQty_Index > 0 && memoItem_price_Index < 0)
                                            {
                                                foreach (var order in memoItem_order_List?.row)
                                                {
                                                    checkpricrisnull = true;
                                                    total_Orders.Add(new OrderModel
                                                    {
                                                        Id = order[memoItem_id_Index].value,
                                                        Amount = double.Parse(order[memoItem__ReceiveQty_Index]?.value ?? "0"),
                                                        Price = 0
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    #endregion

                                    #region จัดกลุ่มใบที่กด ref ในทุก row
                                    total_Orders = total_Orders
                                        .GroupBy(x => x.Id)
                                        .Select(g =>
                                        {
                                            var orderId = g.Key;
                                            var extractMode = g.Key.Count() >= 2;
                                            var totalAmount = g.Sum(x => x.Amount);
                                            var totalPrice = g.Sum(x => x.Price);

                                            return new OrderModel
                                            {
                                                Id = orderId,
                                                Amount = totalAmount,
                                                Price = totalPrice,
                                                ExtractMode = extractMode
                                            };
                                        })
                                        .ToList(); //จัดกลุ่มของ total_Orders
                                    #endregion

                                    #region คำนวนโดยใบที่ถูก ref - ใบที่กด ref 
                                    var orderDifferences = total_Orders
                                        .Join(initial_Orders,
                                              total => total.Id,
                                              initial => initial.Id,
                                              (total, initial) =>
                                              {
                                                  var orderId = total.Id;
                                                  var totalAmount = total.Amount;
                                                  var initialAmount = initial.Amount;
                                                  var initialPrice = initial.Price;
                                                  var totalPrice = total.Price;
                                                  double differencePrice = 0;
                                                  double differenceAmount = 0;
                                                  if (!checkpricrisnull)
                                                  {
                                                      differencePrice = initialPrice - totalPrice;
                                                  }
                                                  if (!checkamountisnull)
                                                  {
                                                      differenceAmount = initialAmount - totalAmount;
                                                  }
                                                  return new OrderModel
                                                  {
                                                      Id = orderId,
                                                      Amount = differenceAmount,
                                                      Price = differencePrice
                                                  };
                                              })
                                        .ToList();
                                    #endregion

                                    var removeOders = orderDifferences.FindAll(x => x.Amount <= 0 && x.Price <= 0);
                                    LogAddon($"result removeOders : {removeOders.ToJson()}", LogModule.RefForm);

                                    foreach (var od in orderDifferences)
                                    {
                                        var rowIndex = AdvanceFormExt.FindRowIndexById(order_List, od.Id);
                                        if (rowIndex == -1) continue;
                                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, amount_Index, od.Amount.ToString()); //replace amount ลงในตาราง
                                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, price_Index, od.Price.ToString()); //replace price ลงในตาราง
                                    }

                                    foreach (var od in removeOders)
                                    {
                                        order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                                    }

                                    LogAddon($"result OrderList : {order_List.ToJson()}", LogModule.RefForm);
                                    memo.MAdvancveForm = AdvanceFormExt.ReplaceDataTable(memo.MAdvancveForm, JObject.Parse(JsonConvert.SerializeObject(order_List)), !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                }
                            }
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        LogAddon($"MemoId Error : {memo.MemoId}", LogModule.RefForm);
                        LogAddon($"{ex}");
                    }
                }
            }

            LogAddon($"Respone Count : {result.Count}", LogModule.RefForm);
            return result;
        }
        public static AddonFormModel.Form_Model MAdvancveFormByMemoIds_Receive(AddonFormModel.Form_Model model)
        {
            try
            {
                using (var db = DBContext.OpenConnection(model.memoPage.memoDetail.connectionString))
                {
                    var Main_template = db.MSTTemplates.Where(x => x.TemplateId == model.memoPage.memoDetail.template_id).FirstOrDefault();
                    var lstmstSetLabelPurchase = db.MSTMasterDatas.Where(
                        x => x.MasterType == "SET_LABEL_PURCHASE" &&
                        x.IsActive == true &&
                        x.Value4.Contains(Main_template.DocumentCode) &&
                        x.Value1 == "Receive"
                        ).FirstOrDefault();
                    if (lstmstSetLabelPurchase != null)
                    {
                        var lstLabelPurchase = lstmstSetLabelPurchase.Value2?.Split('|').ToList();
                        if (lstLabelPurchase != null && lstLabelPurchase.Count == 4)
                        {
                            string memotable = string.Empty;
                            string Qty = string.Empty;
                            string Amount = string.Empty;
                            string ReceiveQty = string.Empty;

                            var memoMain = db.TRNMemoes.Where(e => e.MemoId == model.memoPage.memoDetail.memoid).FirstOrDefault();
                            if (memoMain.StatusName == "Wait for Approve")
                            {
                                var empseq1 = db.TRNLineApproves.Where(x => x.MemoId == memoMain.MemoId && x.Seq == 1).Select(x => x.EmployeeId);
                                if (empseq1.Any(e => e.HasValue && e.Value == memoMain.PersonWaitingId))
                                {
                                    memotable = !string.IsNullOrEmpty(lstLabelPurchase[0]) ? lstLabelPurchase[0] : string.Empty;  //รายการ
                                    Qty = !string.IsNullOrEmpty(lstLabelPurchase[1]) ? lstLabelPurchase[1] : string.Empty;        //จำนวน
                                    Amount = !string.IsNullOrEmpty(lstLabelPurchase[2]) ? lstLabelPurchase[2] : string.Empty;     //จำนวนเงิน
                                    ReceiveQty = !string.IsNullOrEmpty(lstLabelPurchase[3]) ? lstLabelPurchase[3] : string.Empty;

                                    if (db.TRNMemoes.Any(x => memoMain.MemoId == x.MemoId))
                                    {
                                        int? refmemo = db.TRNReferenceDocs.Where(x => x.MemoID == memoMain.MemoId).Select(x => x.MemoRefDocID).FirstOrDefault();
                                        var memo = db.TRNMemoes.Where(x => x.MemoId == refmemo).FirstOrDefault();
                                        var lastrefmemo = db.TRNReferenceDocs.Where(x => x.MemoRefDocID == refmemo)
                                            .Join(db.TRNMemoes,
                                                rm => rm.MemoID,
                                                m => m.MemoId,
                                                (rm, m) => new MemoDto
                                                {
                                                    StatusName = m.StatusName,
                                                    MAdvancveForm = m.MAdvancveForm,
                                                    MemoId = m.MemoId,
                                                    TemplateId = m.TemplateId
                                                })
                                            .Where(x => x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled)
                                            .ToList();

                                        var templateIds = lastrefmemo.Select(x => x.TemplateId).Distinct().ToList();
                                        var templateMap = db.MSTTemplates
                                            .Where(x => templateIds.Contains(x.TemplateId))
                                            .Select(x => new { x.TemplateId, x.DocumentCode })
                                            .ToDictionary(x => x.TemplateId, x => x.DocumentCode);

                                        foreach (var item in lastrefmemo)
                                        {
                                            if (item.TemplateId.HasValue && templateMap.ContainsKey(item.TemplateId.Value))
                                            {
                                                item.DocumentCode = templateMap[item.TemplateId.Value];
                                            }
                                        }
                                        lastrefmemo.RemoveAll(r => !lstmstSetLabelPurchase.Value4.Contains(r.DocumentCode));
                                        LogAddon($"memoRelate count : {lastrefmemo.Count}", LogModule.RefForm);

                                        var initialRequest = new MemoDto
                                        {
                                            MAdvancveForm = memo.MAdvancveForm,
                                            MemoId = memo.MemoId,
                                            TemplateId = memo.TemplateId
                                        };

                                        var initialMemo = initialRequest;
                                        var memos = lastrefmemo.FindAll(x => x.MemoId != initialMemo.MemoId); //memos ใบที่กด ref

                                        #region เก็บค่า Amount และ Price ที่มาจากใบที่ถูก ref ในทุก row
                                        var order_List = AdvanceFormExt.GetDataTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                        var id_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == "id")?.Index ?? -1;
                                        var amount_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Qty)?.Index ?? -1;
                                        var price_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;
                                        var ReceiveQty_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == ReceiveQty)?.Index ?? -1;

                                        var initial_Orders = new List<OrderModel>();
                                        foreach (var order in order_List?.row)
                                        {
                                            LogAddon($"initial_Oerder : {order[id_Index].value} : {order[amount_Index].value}", LogModule.RefForm);
                                            initial_Orders.Add(new OrderModel
                                            {
                                                Id = order[id_Index].value,
                                                Amount = double.Parse(order[amount_Index]?.value ?? "0"),
                                                Price = double.Parse(order[price_Index]?.value ?? "0"),
                                            });
                                        }
                                        #endregion

                                        #region เก็บค่า Amount และ Price ที่มาจากใบที่กด ref ในทุก row
                                        var total_Orders = new List<OrderModel>();
                                        bool checkpricrisnull = false;
                                        bool checkamountisnull = false;
                                        foreach (var memoItem in memos)
                                        {
                                            var memoItem_order_List = AdvanceFormExt.GetDataTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                            if (memoItem_order_List == null)
                                            {
                                                continue;
                                            }
                                            var memoItem_id_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == "id")?.Index ?? 0;
                                            var memoItem_amount_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == Qty)?.Index ?? 0;
                                            var memoItem_price_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;
                                            var memoItem__ReceiveQty_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == ReceiveQty)?.Index ?? -1;
                                            var memoItem_initial_Orders = new List<OrderModel>();
                                            if (memoItem_order_List?.row != null)
                                            {
                                                if (memoItem_id_Index >= 0 && memoItem__ReceiveQty_Index >= 0 && memoItem_price_Index >= 0)
                                                {
                                                    foreach (var order in memoItem_order_List?.row)
                                                    {
                                                        total_Orders.Add(new OrderModel
                                                        {
                                                            Id = order[memoItem_id_Index].value,
                                                            Amount = double.Parse(order[memoItem__ReceiveQty_Index]?.value ?? "0"),
                                                            Price = double.Parse(order[memoItem_price_Index]?.value ?? "0"),
                                                        });
                                                    }
                                                }
                                                else if (memoItem_price_Index < 0 && memoItem__ReceiveQty_Index < 0)
                                                {
                                                    foreach (var order in memoItem_order_List?.row)
                                                    {
                                                        checkpricrisnull = true;
                                                        checkamountisnull = true;
                                                        total_Orders.Add(new OrderModel
                                                        {
                                                            Id = order[memoItem_id_Index].value,
                                                            Amount = 0,
                                                            Price = 0
                                                        });
                                                    }
                                                }
                                                else if (memoItem_price_Index > 0 && memoItem__ReceiveQty_Index < 0)
                                                {
                                                    foreach (var order in memoItem_order_List?.row)
                                                    {
                                                        checkamountisnull = true;
                                                        total_Orders.Add(new OrderModel
                                                        {
                                                            Id = order[memoItem_id_Index].value,
                                                            Amount = 0,
                                                            Price = double.Parse(order[memoItem_price_Index]?.value ?? "0")
                                                        });
                                                    }
                                                }
                                                else if (memoItem__ReceiveQty_Index > 0 && memoItem_price_Index < 0)
                                                {
                                                    foreach (var order in memoItem_order_List?.row)
                                                    {
                                                        checkpricrisnull = true;
                                                        total_Orders.Add(new OrderModel
                                                        {
                                                            Id = order[memoItem_id_Index].value,
                                                            Amount = double.Parse(order[memoItem__ReceiveQty_Index]?.value ?? "0"),
                                                            Price = 0
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                        #endregion

                                        #region จัดกลุ่มใบที่กด ref ในทุก row
                                        total_Orders = total_Orders
                                            .GroupBy(x => x.Id)
                                            .Select(g =>
                                            {
                                                var orderId = g.Key;
                                                var extractMode = g.Key.Count() >= 2;
                                                var totalAmount = g.Sum(x => x.Amount);
                                                var totalPrice = g.Sum(x => x.Price);

                                                return new OrderModel
                                                {
                                                    Id = orderId,
                                                    Amount = totalAmount,
                                                    Price = totalPrice,
                                                    ExtractMode = extractMode
                                                };
                                            })
                                            .ToList(); //จัดกลุ่มของ total_Orders
                                        #endregion

                                        #region คำนวนโดยใบที่ถูก ref - ใบที่กด ref 
                                        var orderDifferences = total_Orders
                                            .Join(initial_Orders,
                                                  total => total.Id,
                                                  initial => initial.Id,
                                                  (total, initial) =>
                                                  {
                                                      var orderId = total.Id;
                                                      var totalAmount = total.Amount;
                                                      var initialAmount = initial.Amount;
                                                      var initialPrice = initial.Price;
                                                      var totalPrice = total.Price;
                                                      double differencePrice = 0;
                                                      double differenceAmount = 0;
                                                      if (!checkpricrisnull)
                                                      {
                                                          differencePrice = initialPrice - totalPrice;
                                                      }
                                                      if (!checkamountisnull)
                                                      {
                                                          differenceAmount = initialAmount - totalAmount;
                                                      }
                                                      return new OrderModel
                                                      {
                                                          Id = orderId,
                                                          Amount = differenceAmount,
                                                          Price = differencePrice
                                                      };
                                                  })
                                            .ToList();
                                        #endregion

                                        var removeOders = orderDifferences.FindAll(x => x.Amount <= 0 && x.Price <= 0);
                                        LogAddon($"result removeOders : {removeOders.ToJson()}", LogModule.RefForm);
                                        foreach (var od in removeOders)
                                        {
                                            order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                                        }
                                        var Rpmemo = db.TRNMemoes.Where(x => x.MemoId == model.memoPage.memoDetail.memoid).FirstOrDefault();
                                        string MAdvancveForm = Rpmemo.MAdvancveForm;
                                        var Mavan = AdvanceFormExt.GetDataTable(MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                        var MIndexamount = AdvanceFormExt.GetAllColumnIndexTable(MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Qty)?.Index ?? -1;
                                        var MIndexprice = AdvanceFormExt.GetAllColumnIndexTable(MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;
                                        var MIndexReceiveQty = AdvanceFormExt.GetAllColumnIndexTable(MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == ReceiveQty)?.Index ?? -1;
                                        foreach (var od in orderDifferences)
                                        {
                                            var rowIndex = AdvanceFormExt.FindRowIndexById(Mavan, od.Id);
                                            if (rowIndex == -1) continue;
                                            Mavan = AdvanceFormExt.ReplaceValueInTable(Mavan, rowIndex, MIndexamount, od.Amount.ToString()); //replace amount ลงในตาราง
                                            Mavan = AdvanceFormExt.ReplaceValueInTable(Mavan, rowIndex, price_Index, od.Price.ToString()); //replace price ลงในตาราง
                                        }
                                        LogAddon($"result OrderList : {Mavan.ToJson()}", LogModule.RefForm);
                                        memoMain.MAdvancveForm = AdvanceFormExt.ReplaceDataTable(MAdvancveForm, JObject.Parse(JsonConvert.SerializeObject(Mavan)), !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                                        Rpmemo.MAdvancveForm = memoMain.MAdvancveForm;
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }
                    }

                }
                return model;
            }
            catch (Exception ex)
            {
                Ext.ErrorLog(ex, "MAdvancveFormByMemoIds_Receive");
                return model;
            }
        }

        public static DT_RTN_API003_SNDItem[] InsertVendorMaster(DT_TLI_API003_SNDRow requestBody)
        {
            try
            {
                string requestData = "";
                string TokenResponse = CoreAPI.CallTokenAPI(requestData);
                JObject jsonObject = JObject.Parse(TokenResponse);
                string token = jsonObject["access_token"].ToString();
                string token_type = jsonObject["token_type"].ToString();

                var binding = new BasicHttpBinding();
                // Set security mode if needed (e.g., Transport for HTTPS)
                binding.Security.Mode = BasicHttpSecurityMode.Transport; // If using HTTPS
                // Endpoint address
                string vendorMasterEndpoint = System.Web.Configuration.WebConfigurationManager.AppSettings["VendorMasterEndpoint"];
                var endpoint = new EndpointAddress(vendorMasterEndpoint);
                // Create the client with binding and endpoint
                var client = new VendorMasterServiceReference.SI_TLI_BPMaster_SYNC_SNDClient(binding, endpoint);
                using (new OperationContextScope(client.InnerChannel))
                {
                    string key = $"{token_type} {token}";
                    LogAddon($"Authorization:{key}");
                    HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();
                    requestMessage.Headers["Authorization"] = key;
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;
                    var request = new List<DT_TLI_API003_SNDRow>();
                    request.Add(requestBody);
                    LogAddon($"call SI_TLI_BPMaster_SYNC_SND data request:{request.ToJson()}");
                    var result = client.SI_TLI_BPMaster_SYNC_SND(request.ToArray());
                    LogAddon($"call SI_TLI_BPMaster_SYNC_SND success.:{result}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                LogAddon($"error exception : {ex}");
                return new List<DT_RTN_API003_SNDItem>().ToArray();
            }
        }

        static string userName = System.Web.Configuration.WebConfigurationManager.AppSettings["UserName"];
        static string passWord = System.Web.Configuration.WebConfigurationManager.AppSettings["PassWord"];
        public static InsertGRCustomResponeModel InsertGRMaster(DT_TLI_AAI001_SNDRow[] requestBody)
        {
            try
            {

                var _ActiveInterface = WebConfigurationManager.AppSettings["ActiveInterface"];
                Boolean blActiveInterface = true;
                if (!String.IsNullOrEmpty(_ActiveInterface))
                    blActiveInterface = _ActiveInterface == "T";



                string requestData = "";
                string TokenResponse = CoreAPI.CallTokenAPI(requestData);
                JObject jsonObject = JObject.Parse(TokenResponse);
                string token = jsonObject["access_token"].ToString();
                string token_type = jsonObject["token_type"].ToString();
                var binding = new BasicHttpBinding();
                // Set security mode if needed (e.g., Transport for HTTPS)
                binding.Security.Mode = BasicHttpSecurityMode.Transport; // If using HTTPS
                // Endpoint address
                string vendorMasterEndpoint = System.Web.Configuration.WebConfigurationManager.AppSettings["GRMasterEndpoint"];
                var endpoint = new EndpointAddress(vendorMasterEndpoint);
                // Create the client with binding and endpoint
                WriteLogFile.LogAddon($"{DateTime.Now} before var client = new GRServiceReference.SI_TLI_PurchaseOnline_SYNC_SNDClient(binding, endpoint);");
                var client = new GRServiceReference.SI_TLI_PurchaseOnline_SYNC_SNDClient(binding, endpoint);
                WriteLogFile.LogAddon($"{DateTime.Now} after var client = new GRServiceReference.SI_TLI_PurchaseOnline_SYNC_SNDClient(binding, endpoint);");
                using (new OperationContextScope(client.InnerChannel))
                {
                    string key = $"{token_type} {token}";
                    LogAddon($"Authorization:{key}");
                    HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();
                    requestMessage.Headers["Authorization"] = key;
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;
                    //var request = new List<DT_TLI_AAI001_SNDRow>();
                    //request.Add(requestBody);

                    LogAddon($"blActiveInterface : {blActiveInterface} | call SI_TLI_PurchaseOnline_SYNC_SND:{requestBody.ToJson()}");
                    var customResponse = new InsertGRCustomResponeModel();
                    if (blActiveInterface)
                    {
                        var result = client.SI_TLI_PurchaseOnline_SYNC_SND(requestBody);
                        var jsonResult = JsonConvert.SerializeObject(result);
                        LogAddon($"call SI_TLI_PurchaseOnline_SYNC_SND success.:{jsonResult}");
                        customResponse.@return = JsonConvert.DeserializeObject<List<ErrorDetail>>(jsonResult);
                        customResponse.Type = customResponse.@return.FirstOrDefault(x => x.Type == "E")?.Type ?? "S";
                        customResponse.Types = string.Join(",", customResponse.@return.Select(e => e.Type));
                        customResponse.Message = string.Join(",", customResponse.@return.Select(e => e.Message));
                        customResponse.Number = string.Join(",", customResponse.@return.Select(e => e.Number));
                    }
                    else
                    {
                        List<ErrorDetail> temp = new List<ErrorDetail>();
                        temp.Add(new ErrorDetail { Type = "I", Message = "$blActiveInterface : {blActiveInterface}" });
                        customResponse.@return = temp;
                        customResponse.Types = "I";
                        customResponse.Message = "$blActiveInterface : {blActiveInterface}";
                        customResponse.Number = "XXXX";
                    }
                    return customResponse;
                }
            }
            catch (Exception ex)
            {
                LogAddon($"error exception : {ex}");
                return new InsertGRCustomResponeModel();
            }
        }

        public static List<RefModel> Filter_E_Procurement_ReferenceDoc(List<RefModel> request, AddonFormModel.MemoDetail requestModel)
        {
            LogAddon($"Before request Count : {request.Count}", LogModule.FltRef);
            var dbContext = DBContext.OpenConnection(requestModel.connectionString);

            var requestIds = request.Select(s => s.MemoId).ToArray();
            var refMemos = dbContext.TRNMemoes.Where(x => requestIds.Contains(x.MemoId)).Select(x => new MAdvancveFormResponeModel
            {
                MemoId = x.MemoId,
                MAdvancveForm = x.MAdvancveForm,
                DocumentNo = x.DocumentNo,
                TemplateId = x.TemplateId ?? 0
            }).ToList();

            var validateMemoOrders = new List<int>();
            foreach (var item in refMemos)
            {
                validateMemoOrders.Add(CheckDataInTable(item, requestModel.connectionString, requestModel.template_id ?? 0));
            }

            request = request.FindAll(x => !validateMemoOrders.Contains(x.MemoId));

            LogAddon($"After request Count : {request.Count}", LogModule.FltRef);

            return request;
        }

        public static int CheckDataInTable(MAdvancveFormResponeModel request, string connectionString, int tempRequestId)
        {
            try
            {
                var db = DBContext.OpenConnection(connectionString);
                #region Non-Receive
                var mstTempCancelControl = db.MSTMasterDatas.FirstOrDefault(x => x.MasterType == "TEMPCODE_CANCEL");
                var prCancel = mstTempCancelControl.Value1.Split(',').ToList();
                var poCancel = mstTempCancelControl.Value2.Split(',').ToList();
                var grCancel = mstTempCancelControl.Value3.Split(',').ToList();
                var prCancelIds = db.MSTTemplates.Where(x => prCancel.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
                var poCancelIds = db.MSTTemplates.Where(x => poCancel.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
                var grCancelIds = db.MSTTemplates.Where(x => grCancel.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
                List<int?> templateCancel = new List<int?>();
                if (prCancelIds.Any()) templateCancel.AddRange(prCancelIds);
                if (poCancelIds.Any()) templateCancel.AddRange(poCancelIds);
                if (grCancelIds.Any()) templateCancel.AddRange(grCancelIds);


                var Main_template = db.MSTTemplates.Where(x => x.TemplateId == tempRequestId).FirstOrDefault();
                string memotable = string.Empty;
                string Qty = string.Empty;
                string Amount = string.Empty;
                string ReceiveQty = string.Empty;
                var lstmstSetLabelPurchase = db.MSTMasterDatas.Where(
                    x => x.MasterType == "SET_LABEL_PURCHASE" &&
                    x.Value1 == "Non-Receive" &&
                    x.Value4.Contains(Main_template.DocumentCode) &&
                    x.IsActive == true).FirstOrDefault();

                if (lstmstSetLabelPurchase != null)
                {
                    var lstLabelPurchase = lstmstSetLabelPurchase.Value2?.Split('|').ToList();
                    if (lstLabelPurchase != null && lstLabelPurchase.Count == 3)
                    {
                        memotable = !string.IsNullOrEmpty(lstLabelPurchase[0]) ? lstLabelPurchase[0] : string.Empty;  //รายการ
                        Qty = !string.IsNullOrEmpty(lstLabelPurchase[1]) ? lstLabelPurchase[1] : string.Empty;        //จำนวน
                        Amount = !string.IsNullOrEmpty(lstLabelPurchase[2]) ? lstLabelPurchase[2] : string.Empty;
                    }

                    var memoRelate = db.TRNReferenceDocs.Where(x => x.MemoRefDocID == request.MemoId)
                        .Join(db.TRNMemoes.Where(x => x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled),
                        rm => rm.MemoID,
                        m => m.MemoId,
                        (rm, m) => new MemoDto
                        {
                            StatusName = m.StatusName,
                            MAdvancveForm = m.MAdvancveForm,
                            MemoId = m.MemoId,
                            TemplateId = m.TemplateId
                        }).ToList();

                    var initialRequest = new MemoDto
                    {
                        MAdvancveForm = request.MAdvancveForm,
                        MemoId = request.MemoId,
                        TemplateId = request.TemplateId
                    };

                    var initialMemo = /*chkClosingRequestTemp ? initialClosingCase :*/ initialRequest;

                    List<int?> removeref = new List<int?>();
                    if (memoRelate.Any())
                    {
                        var refMemoIds = memoRelate.SelectMany(itemmm => db.TRNReferenceDocs.Where(x => x.MemoRefDocID == itemmm.MemoId).Select(x => x.MemoID)).ToList();
                        removeref = db.TRNMemoes.Where(x => refMemoIds.Contains(x.MemoId) && templateCancel.Contains(x.TemplateId) && x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled).Select(x => (int?)x.MemoId).ToList();
                        if (removeref.Any())
                        {
                            var remove = db.TRNReferenceDocs.Where(x => removeref.Contains(x.MemoID)).Select(x => x.MemoRefDocID).ToList();
                            memoRelate.RemoveAll(r => remove.Contains(r.MemoId));
                        }
                    }

                    var memos = memoRelate.FindAll(x => x.MemoId != initialMemo.MemoId);
                    var order_List = AdvanceFormExt.GetDataTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                    var id_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == "id")?.Index ?? -1;
                    var amount_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Qty)?.Index ?? -1;
                    var price_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;

                    if (id_Index == -1 || amount_Index == -1) return 0;

                    var initial_Orders = new List<OrderModel>();
                    if (order_List?.row != null)
                    {
                        foreach (var order in order_List?.row)
                        {
                            LogAddon($"initial_Oerder : {order[id_Index].value} : {order[amount_Index].value}", LogModule.RefForm);
                            initial_Orders.Add(new OrderModel
                            {
                                Id = order[id_Index].value,
                                Amount = double.Parse(order[amount_Index]?.value ?? "0"),
                                Price = double.Parse(order[price_Index]?.value ?? "0"),
                            });
                        }
                    }

                    var total_Orders = new List<OrderModel>();
                    bool checkpricrisnull = false;
                    bool checkamountisnull = false;

                    foreach (var memoItem in memos)
                    {
                        var memoItem_order_List = AdvanceFormExt.GetDataTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                        if (memoItem_order_List == null)
                        {
                            continue;
                        }
                        var memoItem_id_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == "id")?.Index ?? 0;
                        var memoItem_amount_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == Qty)?.Index ?? 0;
                        var memoItem_price_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;

                        var memoItem_initial_Orders = new List<OrderModel>();
                        if (memoItem_order_List?.row != null)
                        {
                            if (memoItem_id_Index >= 0 && memoItem_amount_Index >= 0 && memoItem_price_Index >= 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = double.Parse(order[memoItem_amount_Index]?.value ?? "0"),
                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0"),
                                    });
                                }
                            }
                            else if (memoItem_price_Index < 0 && memoItem_amount_Index < 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    checkpricrisnull = true;
                                    checkamountisnull = true;
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = 0,
                                        Price = 0
                                    });
                                }
                            }
                            else if (memoItem_price_Index > 0 && memoItem_amount_Index < 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    checkamountisnull = true;
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = 0,
                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0")
                                    });
                                }
                            }
                            else if (memoItem_amount_Index > 0 && memoItem_price_Index < 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    checkpricrisnull = true;
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = double.Parse(order[memoItem_amount_Index]?.value ?? "0"),
                                        Price = 0
                                    });
                                }
                            }
                        }
                    }

                    initial_Orders = initial_Orders
                        .GroupBy(x => x.Id)
                        .Select(g =>
                        {
                            var orderId = g.Key;
                            var totalAmount = g.Sum(x => x.Amount);
                            var totalPrice = g.Sum(x => x.Price);
                            return new OrderModel
                            {
                                Id = orderId,
                                Amount = totalAmount,
                                Price = totalPrice
                            };
                        })
                        .ToList();

                    total_Orders = total_Orders
                        .GroupBy(x => x.Id)
                        .Select(g =>
                        {
                            var orderId = g.Key;
                            var totalAmount = g.Sum(x => x.Amount);
                            var totalPrice = g.Sum(x => x.Price);

                            return new OrderModel
                            {
                                Id = orderId,
                                Amount = totalAmount,
                                Price = totalPrice
                            };
                        })
                        .ToList();

                    var orderDifferences = total_Orders
                        .Join(initial_Orders,
                              total => total.Id,
                              initial => initial.Id,
                              (total, initial) =>
                              {
                                  var orderId = total.Id;
                                  var totalAmount = total.Amount;
                                  var initialAmount = initial.Amount;
                                  var initialPrice = initial.Price;
                                  var totalPrice = total.Price;
                                  double differencePrice = 0;
                                  double differenceAmount = 0;
                                  if (!checkpricrisnull)
                                  {
                                      differencePrice = initialPrice - totalPrice;
                                  }
                                  if (!checkamountisnull)
                                  {
                                      differenceAmount = initialAmount - totalAmount;
                                  }

                                  return new OrderModel
                                  {
                                      Id = orderId,
                                      Amount = differenceAmount,
                                      Price = differencePrice
                                  };
                              })
                        .ToList();

                    var removeOders = orderDifferences.FindAll(x => x.Amount <= 0 || x.Price <= 0);

                    foreach (var od in orderDifferences)
                    {
                        var rowIndex = AdvanceFormExt.FindRowIndexById(order_List, od.Id);
                        if (rowIndex == -1) continue;
                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, amount_Index, od.Amount.ToString());
                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, price_Index, od.Price.ToString());
                    }

                    foreach (var od in removeOders)
                    {
                        order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                    }

                    if (order_List.row != null && !order_List.row.Any())
                    {
                        return request.MemoId;
                    }
                }
                #endregion
                #region Receive
                var lstmstSetLabelPurchases = db.MSTMasterDatas.Where(
                    x => x.MasterType == "SET_LABEL_PURCHASE" &&
                    x.Value1 == "Receive" &&
                    x.Value4.Contains(Main_template.DocumentCode) &&
                    x.IsActive == true).FirstOrDefault();

                if (lstmstSetLabelPurchases != null)
                {
                    var lstLabelPurchase = lstmstSetLabelPurchases.Value2?.Split('|').ToList();
                    if (lstLabelPurchase != null && lstLabelPurchase.Count == 4)
                    {
                        memotable = !string.IsNullOrEmpty(lstLabelPurchase[0]) ? lstLabelPurchase[0] : string.Empty;  //รายการ
                        Qty = !string.IsNullOrEmpty(lstLabelPurchase[1]) ? lstLabelPurchase[1] : string.Empty;        //จำนวน
                        Amount = !string.IsNullOrEmpty(lstLabelPurchase[2]) ? lstLabelPurchase[2] : string.Empty;
                        ReceiveQty = !string.IsNullOrEmpty(lstLabelPurchase[3]) ? lstLabelPurchase[3] : string.Empty;
                    }

                    var memoRelate = db.TRNReferenceDocs.Where(x => x.MemoRefDocID == request.MemoId)
                        .Join(db.TRNMemoes.Where(x => x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled),
                        rm => rm.MemoID,
                        m => m.MemoId,
                        (rm, m) => new MemoDto
                        {
                            StatusName = m.StatusName,
                            MAdvancveForm = m.MAdvancveForm,
                            MemoId = m.MemoId,
                            TemplateId = m.TemplateId
                        }).ToList();

                    var initialRequest = new MemoDto
                    {
                        MAdvancveForm = request.MAdvancveForm,
                        MemoId = request.MemoId,
                        TemplateId = request.TemplateId
                    };

                    var initialMemo = /*chkClosingRequestTemp ? initialClosingCase :*/ initialRequest;

                    List<int?> removeref = new List<int?>();
                    if (memoRelate.Any())
                    {
                        var refMemoIds = memoRelate.SelectMany(itemmm => db.TRNReferenceDocs.Where(x => x.MemoRefDocID == itemmm.MemoId).Select(x => x.MemoID)).ToList();
                        removeref = db.TRNMemoes.Where(x => refMemoIds.Contains(x.MemoId) && templateCancel.Contains(x.TemplateId) && x.StatusName != Ext.Status._Rejected && x.StatusName != Ext.Status._Cancelled).Select(x => (int?)x.MemoId).ToList();
                        if (removeref.Any())
                        {
                            var remove = db.TRNReferenceDocs.Where(x => removeref.Contains(x.MemoID)).Select(x => x.MemoRefDocID).ToList();
                            memoRelate.RemoveAll(r => remove.Contains(r.MemoId));
                        }
                    }

                    var memos = memoRelate.FindAll(x => x.MemoId != initialMemo.MemoId);
                    var order_List = AdvanceFormExt.GetDataTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                    var id_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == "id")?.Index ?? -1;
                    var amount_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Qty)?.Index ?? -1;
                    var price_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;
                    var ReceiveQty_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == ReceiveQty)?.Index ?? -1;

                    if (id_Index == -1 || amount_Index == -1) return 0;

                    var initial_Orders = new List<OrderModel>();
                    if (order_List?.row != null)
                    {
                        foreach (var order in order_List?.row)
                        {
                            LogAddon($"initial_Oerder : {order[id_Index].value} : {order[amount_Index].value}", LogModule.RefForm);
                            initial_Orders.Add(new OrderModel
                            {
                                Id = order[id_Index].value,
                                Amount = double.Parse(order[amount_Index]?.value ?? "0"),
                                Price = double.Parse(order[price_Index]?.value ?? "0"),
                            });
                        }
                    }

                    var total_Orders = new List<OrderModel>();
                    bool checkpricrisnull = false;
                    bool checkamountisnull = false;
                    foreach (var memoItem in memos)
                    {
                        var memoItem_order_List = AdvanceFormExt.GetDataTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ");
                        if (memoItem_order_List == null)
                        {
                            continue;
                        }
                        var memoItem_id_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == "id")?.Index ?? 0;
                        var memoItem_amount_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ")?.FirstOrDefault(x => x.Name == Qty)?.Index ?? 0;
                        var memoItem_price_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == Amount)?.Index ?? -1;
                        var memoItem__ReceiveQty_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, !string.IsNullOrEmpty(memotable) ? memotable : "รายการ").FirstOrDefault(x => x.Name == ReceiveQty)?.Index ?? -1;

                        var memoItem_initial_Orders = new List<OrderModel>();
                        if (memoItem_order_List?.row != null)
                        {
                            if (memoItem_id_Index >= 0 && memoItem__ReceiveQty_Index >= 0 && memoItem_price_Index >= 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = double.Parse(order[memoItem__ReceiveQty_Index]?.value ?? "0"),
                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0"),
                                    });
                                }
                            }
                            else if (memoItem_price_Index < 0 && memoItem__ReceiveQty_Index < 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    checkpricrisnull = true;
                                    checkamountisnull = true;
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = 0,
                                        Price = 0
                                    });
                                }
                            }
                            else if (memoItem_price_Index > 0 && memoItem__ReceiveQty_Index < 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    checkamountisnull = true;
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = 0,
                                        Price = double.Parse(order[memoItem_price_Index]?.value ?? "0")
                                    });
                                }
                            }
                            else if (memoItem__ReceiveQty_Index > 0 && memoItem_price_Index < 0)
                            {
                                foreach (var order in memoItem_order_List?.row)
                                {
                                    checkpricrisnull = true;
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = double.Parse(order[memoItem__ReceiveQty_Index]?.value ?? "0"),
                                        Price = 0
                                    });
                                }
                            }
                        }

                    }

                    initial_Orders = initial_Orders
                        .GroupBy(x => x.Id)
                        .Select(g =>
                        {
                            var orderId = g.Key;
                            var totalAmount = g.Sum(x => x.Amount);
                            var totalPrice = g.Sum(x => x.Price);
                            return new OrderModel
                            {
                                Id = orderId,
                                Amount = totalAmount,
                                Price = totalPrice
                            };
                        })
                        .ToList();

                    total_Orders = total_Orders
                        .GroupBy(x => x.Id)
                        .Select(g =>
                        {
                            var orderId = g.Key;
                            var totalAmount = g.Sum(x => x.Amount);
                            var totalPrice = g.Sum(x => x.Price);

                            return new OrderModel
                            {
                                Id = orderId,
                                Amount = totalAmount,
                                Price = totalPrice
                            };
                        })
                        .ToList();

                    var orderDifferences = total_Orders
                        .Join(initial_Orders,
                              total => total.Id,
                              initial => initial.Id,
                              (total, initial) =>
                              {
                                  var orderId = total.Id;
                                  var totalAmount = total.Amount;
                                  var initialAmount = initial.Amount;
                                  var initialPrice = initial.Price;
                                  var totalPrice = total.Price;
                                  double differencePrice = 0;
                                  double differenceAmount = 0;
                                  if (!checkpricrisnull)
                                  {
                                      differencePrice = initialPrice - totalPrice;
                                  }
                                  if (!checkamountisnull)
                                  {
                                      differenceAmount = initialAmount - totalAmount;
                                  }
                                  return new OrderModel
                                  {
                                      Id = orderId,
                                      Amount = differenceAmount,
                                      Price = differencePrice
                                  };
                              })
                        .ToList();

                    var removeOders = orderDifferences.FindAll(x => x.Amount <= 0 || x.Price <= 0);

                    foreach (var od in orderDifferences)
                    {
                        var rowIndex = AdvanceFormExt.FindRowIndexById(order_List, od.Id);
                        if (rowIndex == -1) continue;
                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, amount_Index, od.Amount.ToString());
                        order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, price_Index, od.Price.ToString());
                    }

                    foreach (var od in removeOders)
                    {
                        order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                    }

                    if (order_List.row != null && !order_List.row.Any())
                    {
                        return request.MemoId;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                LogAddon("memoid " + request.MemoId);
                LogAddon(ex.ToString());
            }
            return 0;
        }

        //public static string IsDuplicateInMemo(AddonFormModel.MemoPage request)
        //{
        //    try
        //    {
        //        using (WolfApproveModel db = DBContext.OpenConnection(request.memoDetail.connectionString))
        //        {
        //            var config = db.MSTMasterDatas.FirstOrDefault(x => x.MasterType.ToUpper() == "DUP_VALUE" && x.IsActive == true);
        //            if (config != null)
        //            {
        //                var InvoiceLabel = config.Value1;
        //                var invoiceDateLabel = config.Value2;
        //                var vendorName = config.Value3;
        //                var documentCodes = config.Value4.Split(',').ToList();
        //                LogAddon($"IsDuplicateInMemo config : {config.Value1} {config.Value2} {config.Value3} {config.Value4}");
        //                var templateIds = db.MSTTemplates.Where(x => documentCodes.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
        //                if (templateIds.Contains(request.memoDetail.template_id))
        //                {
        //                    var advanceFormModel = AdvanceFormExt.ToList(request.memoDetail.template_desc);
        //                    var InvoiceValue = advanceFormModel.FirstOrDefault(x => x.label == InvoiceLabel)?.value;
        //                    var vendorNameValue = advanceFormModel.FirstOrDefault(x => x.label == vendorName)?.value;
        //                    var invoiceDate = DateTimeHelper.ConvertStringToDateTime(advanceFormModel.FirstOrDefault(x => x.label == invoiceDateLabel)?.value) ?? DateTime.MinValue;
        //                    LogAddon($"IsDuplicateInMemo Value : {InvoiceValue} {vendorNameValue} {invoiceDate:dd MMM yyyy}");
        //                    var resultValid = db.TRNMemoes.Where(x =>
        //                    x.MemoId != request.memoDetail.memoid &&
        //                    x.StatusName != Ext.Status._Rejected &&
        //                    x.StatusName != Ext.Status._Cancelled &&
        //                    x.TemplateId == request.memoDetail.template_id &&
        //                    templateIds.Contains(x.TemplateId))
        //                    .GroupJoin(db.TRNMemoForms,
        //                        memo => memo.MemoId,
        //                        mForm => mForm.MemoId,
        //                        (memo, mForms) => new { Memo = memo, MemoForms = mForms })
        //                    .Where(result =>
        //                        result.MemoForms.Any(x => x.obj_label == InvoiceLabel && x.obj_value == InvoiceValue) &&
        //                        result.MemoForms.Any(x => x.obj_label == vendorName && x.obj_value == vendorNameValue) &&
        //                        result.MemoForms.Any(x => x.obj_label == invoiceDateLabel && x.obj_value.Contains(invoiceDate.Year.ToString()))
        //                    )
        //                    .Count();

        //                    LogAddon($"IsDuplicateInMemo Count : {resultValid}");

        //                    if (resultValid >= 1)
        //                    {
        //                        LogAddon($"IsDuplicateInMemo Value5 : {config.Value5}");

        //                        return config.Value5;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogAddon($"IsDuplicateInMemo Error : {ex}");
        //    }

        //    return "";
        //}

        //public static string DuplicateProductId(AddonFormModel.MemoPage request)
        //{
        //    var db = DBContext.OpenConnection(request.memoDetail.connectionString);
        //    var config = db.MSTMasterDatas.FirstOrDefault(x => x.MasterType.ToUpper() == "DUP_PRDITEM" && x.IsActive == true);
        //    if (config != null)
        //    {
        //        var prdLabelTables = config.Value2.Split(',').ToList();
        //        var documentCodes = config.Value3.Split(',').ToList();
        //        var message = config.Value5;
        //        var templateIds = db.MSTTemplates.Where(x => documentCodes.Contains(x.DocumentCode)).Select(s => s.TemplateId).ToList();
        //        if (templateIds.Contains(request.memoDetail.template_id))
        //        {
        //            var advanceFormModel = AdvanceFormExt.ToList(request.memoDetail.template_desc);
        //            var prdValue = new List<string>();

        //            var prdInTables = advanceFormModel.FirstOrDefault(x => x.label == prdLabelTables.First()).row
        //                .SelectMany(s => s)
        //                .Where(x => x.label == prdLabelTables.Last())
        //                .Select(s => s.value).ToList();

        //            prdValue.AddRange(prdInTables);
        //            prdValue.RemoveAll(r => string.IsNullOrEmpty(r) || string.IsNullOrWhiteSpace(r));

        //            var dupValues = db.MSTCategoryProduct.Where(x => prdValue.Contains(x.ItemID))
        //                .Select(s => s.ItemID)
        //                .ToList();
        //            if (dupValues.Any())
        //            {
        //                return config.Value5;
        //            }
        //        }
        //    }

        //    return "";
        //}

        //public static List<BudgetReportItem> FilterReport(
        //List<BudgetReportItem> data, string costKey, string glKey, string ioKey)
        //{
        //    var filteredResult = data
        //        .Where(item => string.IsNullOrEmpty(costKey) || item.Desc.Contains(costKey))
        //        .Select(item =>
        //        {
        //            item.Children = item.Children
        //                .Where(gl => (string.IsNullOrEmpty(glKey) || gl.Desc.Contains(glKey)))
        //                .Select(gl =>
        //                {
        //                    gl.Children = gl.Children.Where(io => (string.IsNullOrEmpty(ioKey) || io.Desc.Contains(ioKey)))
        //                    .OrderByDescending(o => o.Summary_Alloc)
        //                    .ToList();

        //                    return gl;
        //                })
        //                .Where(gl => gl.Children.Any() || string.IsNullOrEmpty(ioKey))
        //                .OrderByDescending(o => o.Summary_Alloc)
        //                .ToList();

        //            return item;
        //        })
        //        .Where(item => item.Children.Any() || (string.IsNullOrEmpty(glKey) && string.IsNullOrEmpty(ioKey)))
        //        .OrderByDescending(o => o.Summary_Alloc)
        //        .ToList();

        //    return filteredResult;
        //}

        //public static List<BudgetReportItem> FilterReportRecursive(List<BudgetReportItem> data, string descKeyword)
        //{
        //    return data
        //        .Select(item => FilterItemWithDescription(item, descKeyword))
        //        .Where(item => item != null)
        //        .ToList();
        //}

        //private static BudgetReportItem FilterItemWithDescription(BudgetReportItem item, string descKeyword)
        //{
        //    bool hasMatchingDescription = !string.IsNullOrEmpty(descKeyword) && item.Desc != null && item.Desc.Contains(descKeyword);

        //    var filteredChildren = item.Children
        //        .Select(child => FilterItemWithDescription(child, descKeyword))
        //        .Where(child => child != null)
        //        .ToList();

        //    if (hasMatchingDescription || filteredChildren.Any())
        //    {
        //        return new BudgetReportItem
        //        {
        //            Key = item.Key,
        //            Desc = item.Desc,
        //            MemoId = item.MemoId,
        //            Summary_Alloc = item.Summary_Alloc,
        //            Summary_Remaining = item.Summary_Remaining,
        //            Summary_Paid = item.Summary_Paid,
        //            Summary_Reserve = item.Summary_Reserve,
        //            Summary_Used = item.Summary_Used,
        //            Type = item.Type,
        //            Children = hasMatchingDescription ? item.Children : filteredChildren
        //        };
        //    }

        //    return null;
        //}

        public static string CleanText(string input)
        {
            return input.Trim().Replace(Environment.NewLine, "");
        }

        public static string CheckRequireBudget(RequestCheckBudget request)
        {
            foreach (var order in request.Orders)
            {
                if (string.IsNullOrEmpty(order.BudgetYear))
                {
                    return "BudgetYear is required but was null or empty.";
                }
                if (string.IsNullOrEmpty(order.Costcenter))
                {
                    return "Costcenter is required but was null or empty.";
                }
                if (string.IsNullOrEmpty(order.GLAccount))
                {
                    return "GLAccount is required but was null or empty.";
                }
            }
            return string.Empty;
        }

        public static bool CheckBudgetTeam(string userPrincipalName)
        {
            string ConnectionString = "data source=DESKTOP-MTFBHTV\\SQLEXPRESS;initial catalog=WolfapproveCore.Dfarm-Dev;persist security info=True;user id=sa;password=pass@word1;";
            var _context = TLIContext.OpenConnection(ConnectionString);
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

            return isBudgetTeam;
        }
    }
}