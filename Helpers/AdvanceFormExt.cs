using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfApprove.API2.Extension;
using System.Web.Http.Results;
using static AddonProcurement.Helpers.AdvanceFormExt;
using System.Web.UI.WebControls;

namespace AddonProcurement.Helpers
{
    public class AdvanceFormExt
    {
        public class RootObject
        {
            public List<Item> Items { get; set; }
        }

        public class Item
        {
            public string Id { get; set; }
            public List<Layout> Layout { get; set; }
        }

        public class Layout
        {
            public Template Template { get; set; }
            public object Data { get; set; }
            public string Guid { get; set; }
            public bool IsShow { get; set; } = true;
            public bool isDirty { get; set; } = false;
            public bool isReadOnly { get; set; }
        }

        public class Template
        {
            public string Type { get; set; }
            public string Label { get; set; }
            public string Alter { get; set; }
            public Attribute Attribute { get; set; }
        }

        public class Attribute
        {
            public string Require { get; set; }
            public string Description { get; set; }
            public string Length { get; set; }
            public string Default { get; set; }
            public string Readonly { get; set; }
            public Date Date { get; set; }
            public Time Time { get; set; }
            public List<Column> column { get; set; }
        }

        public class Column
        {
            public string label { get; set; }
            public string alter { get; set; }
        }

        public class Control
        {
            public Template template { get; set; }
        }

        public class Date
        {
            public string Use { get; set; }
            public string UseDate { get; set; }
            public string FullYear { get; set; }
            public string Symbol { get; set; }
        }

        public class Time
        {
            public string Use { get; set; }
            public string UseSecond { get; set; }
            public string Symbol { get; set; }
        }

        public class Data
        {
            public string value { get; set; }
            public List<List<Row>> row { get; set; }
        }
        public class DataControl
        {
            public Value value { get; set; }
        }
        public class Value
        {
            public List<Item> Items { get; set; }
        }
        public class Row
        {
            public string label { get; set; }
            public string value { get; set; }
            public bool isDirty { get; set; } = false;
        }

        public class AdvanceForm
        {
            public string type { get; set; }
            public string alter { get; set; }
            public string guid { get; set; }
            public string label { get; set; }
            public string value { get; set; }
            public bool isDirty { get; set; } = false;
            public List<List<AdvanceFormRow>> row { get; set; }
        }

        public class AdvanceFormRow
        {
            public string label { get; set; }
            public string value { get; set; }
        }

        public class ColumnModel
        {
            public string Name { get; set; }
            public int Index { get; set; }
        }

        public class RowModel
        {
            public int Index { get; set; }
            public int ColIndex { get; set; }
            public string Value { get; set; }
        }

        public static List<AdvanceForm> ToList(string memo)
        {
            List<AdvanceForm> listadvance = new List<AdvanceForm>();

            if (!string.IsNullOrEmpty(memo))
            {
                var data = JsonConvert.DeserializeObject<RootObject>(memo);

                foreach (var item in data.Items)
                {
                    foreach (var component in item.Layout)
                    {
                        List<List<AdvanceFormRow>> row = new List<List<AdvanceFormRow>>();

                        string guid = component?.Guid;
                        string label = component?.Template?.Label;
                        string type = component?.Template?.Type;
                        var value = "";

                        try
                        {
                            var dataValue = JsonConvert.DeserializeObject<Data>(component?.Data?.ToString());

                            var itemData = dataValue;

                            value = itemData.value;

                            if (itemData.row != null && itemData.row.Any())
                            {
                                if (component.Template != null && component.Template.Attribute != null && component.Template.Attribute.column.Any())
                                {
                                    var column = component.Template.Attribute.column;
                                    foreach (var itemrow in itemData.row)
                                    {
                                        int index = 0;
                                        var listRow = new List<AdvanceFormRow>();
                                        foreach (var itemx in itemrow)
                                        {
                                            itemx.label = column[index].label;
                                            listRow.Add(new AdvanceFormRow
                                            {
                                                label = itemx.label,
                                                value = itemx.value,
                                            });
                                            index++;
                                        }

                                        row.Add(listRow);
                                    }
                                }
                            }

                            listadvance.Add(new AdvanceForm
                            {
                                alter = component?.Template?.Alter,
                                label = label,
                                value = value,
                                row = row,
                                guid = guid,
                                type = type,
                                isDirty = component.isDirty
                            });
                        }
                        catch
                        {
                            try
                            {
                                var dataValue = JsonConvert.DeserializeObject<List<Data>>(component?.Data?.ToString());

                                var listItemData = dataValue;

                                foreach (var itemData in listItemData)
                                {
                                    value = itemData.value;

                                    if (itemData.row != null && itemData.row.Any())
                                    {
                                        if (component.Template != null && component.Template.Attribute != null && component.Template.Attribute.column.Any())
                                        {
                                            var column = component.Template.Attribute.column;
                                            foreach (var rows in itemData.row)
                                            {
                                                int index = 0;
                                                var listRow = new List<AdvanceFormRow>();
                                                foreach (var itemx in rows)
                                                {
                                                    itemx.label = column[index].label;
                                                    listRow.Add(new AdvanceFormRow
                                                    {
                                                        label = itemx.label,
                                                        value = itemx.value,
                                                    });
                                                    index++;
                                                }

                                                row.Add(listRow);
                                            }
                                        }
                                    }

                                    listadvance.Add(new AdvanceForm
                                    {
                                        label = label,
                                        value = value,
                                        row = row,
                                        guid = guid,
                                        type = type,
                                        isDirty = component.isDirty
                                    });
                                }
                            }

                            catch
                            {

                            }
                        }
                    }
                }
            }

            return listadvance;
        }

        public static string RemoveIsDirty(string advanceForm)
        {
            JObject jsonAdvanceForm = JObject.Parse(advanceForm);
            if (jsonAdvanceForm.ContainsKey("items"))
            {
                JArray itemsArray = (JArray)jsonAdvanceForm["items"];
                foreach (JObject jItems in itemsArray)
                {
                    JArray jLayoutArray = (JArray)jItems["layout"];
                    foreach (JToken jLayout in jLayoutArray)
                    {
                        JObject layoutObj = (JObject)jLayout;
                        if (layoutObj.ContainsKey("isDirty"))
                        {
                            layoutObj.Remove("isDirty");
                        }
                        else
                        {
                            if (layoutObj["template"] != null && layoutObj?["template"]?["type"]?.ToString() == "tb")
                            {
                                JObject setValue = (JObject)jLayout?["data"];
                                if (setValue?["row"].HasValues ?? false)
                                {
                                    JArray rows = (JArray)setValue?["row"];
                                    foreach (JToken jRow in rows)
                                    {
                                        foreach (JObject jRowCol in jRow)
                                        {
                                            if (jRowCol.ContainsKey("isDirty"))
                                            {
                                                jRowCol.Remove("isDirty");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(jsonAdvanceForm);
        }

        public static List<RowModel> FindDirtyInTable(Data table)
        {
            var result = new List<RowModel>();

            int rowIndex = 0;
            if (table.row == null)
            {
                return result;
            }
            foreach (var row in table.row)
            {
                int colIndex = 0;
                foreach (var item in row)
                {
                    if (item.isDirty)
                    {
                        result.Add(new RowModel { Index = rowIndex, ColIndex = colIndex, Value = item.value });
                    }
                    colIndex++;
                }
                rowIndex++;
            }

            return result;
        }

        public static List<AdvanceForm> FindDirtyInForm(string advanceForm)
        {
            var result = new List<AdvanceForm>();
            var formModel = JsonConvert.DeserializeObject<RootObject>(advanceForm);
            foreach (var item in formModel.Items)
            {
                foreach (var layout in item.Layout)
                {
                    if (layout.isDirty)
                    {
                        var dataValue = JsonConvert.DeserializeObject<Data>(layout.Data.ToString());
                        result.Add(new AdvanceForm { label = layout.Template.Label, value = dataValue.value, type = layout.Template.Type });
                    }
                }
            }
            return result;
        }

        public static int FindRowIndexById(Data table, string id)
        {
            int rowIndex = 0;

            foreach (var row in table.row)
            {
                foreach (var item in row)
                {
                    if (item.value == id)
                    {
                        return rowIndex;
                    }
                }
                rowIndex++;
            }

            return -1;
        }

        public static List<ColumnModel> GetAllColumnIndexTable(string advanceForm, string label)
        {
            var result = new List<ColumnModel>();
            JObject jsonAdvanceForm = JObject.Parse(advanceForm);
            if (jsonAdvanceForm.ContainsKey("items"))
            {
                JArray itemsArray = (JArray)jsonAdvanceForm["items"];
                foreach (JObject jItems in itemsArray)
                {
                    JArray jLayoutArray = (JArray)jItems["layout"];
                    foreach (JToken jLayout in jLayoutArray)
                    {
                        JObject jTemplate = (JObject)jLayout["template"];
                        var getLabel = (String)jTemplate["label"];
                        if (label == getLabel)
                        {
                            JObject attribute = (JObject)jTemplate["attribute"];
                            var columns = JsonConvert.DeserializeObject<List<Column>>(attribute["column"].ToString());
                            return columns.Select((x, i) => new ColumnModel { Name = x.label, Index = i }).ToList();
                        }
                    }
                }
            }

            return new List<ColumnModel>();
        }

        public static Data ReplaceValueInTable(Data table, int rowIndex, int columnIndex, string value)
        {
            table.row[rowIndex][columnIndex].value = value;
            return table;
        }

        public static Data RemoveRowById(Data table, string id)
        {
            var filteredData = table.row.Where(innerList => !innerList.Any(item => item.value == id)).ToList();
            table.row = filteredData;
            return table;
        }

        public static List<Column> GetColumnInTable(string advanceForm, string label)
        {
            JObject jsonAdvanceForm = JObject.Parse(advanceForm);
            if (jsonAdvanceForm.ContainsKey("items"))
            {
                JArray itemsArray = (JArray)jsonAdvanceForm["items"];
                foreach (JObject jItems in itemsArray)
                {
                    JArray jLayoutArray = (JArray)jItems["layout"];
                    foreach (JToken jLayout in jLayoutArray)
                    {
                        JObject jTemplate = (JObject)jLayout["template"];
                        var getLabel = (String)jTemplate["label"];
                        if (label == getLabel)
                        {
                            JObject attribute = (JObject)jTemplate["attribute"];
                            return JsonConvert.DeserializeObject<List<Column>>(attribute["column"].ToString());
                        }
                    }
                }
            }

            return new List<Column>();
        }

        public static Data GetDataTable(string advanceForm, string label)
        {
            string setValue = "";
            JObject jsonAdvanceForm = JObject.Parse(advanceForm);
            if (jsonAdvanceForm.ContainsKey("items"))
            {
                JArray itemsArray = (JArray)jsonAdvanceForm["items"];
                foreach (JObject jItems in itemsArray)
                {
                    JArray jLayoutArray = (JArray)jItems["layout"];
                    foreach (JToken jLayout in jLayoutArray)
                    {
                        JObject jTemplate = (JObject)jLayout["template"];
                        var getLabel = (String)jTemplate["label"];
                        if (label == getLabel)
                        {
                            setValue = jLayout["data"].ToString();
                        }
                    }
                }
            }

            return JsonConvert.DeserializeObject<Data>(setValue);
        }

        public static string ReplaceDataTable(string advanceForm, JObject Value, string label)
        {
            JObject jsonAdvanceForm = JObject.Parse(advanceForm);
            JArray itemsArray = (JArray)jsonAdvanceForm["items"];
            foreach (JObject jItems in itemsArray)
            {
                JArray jLayoutArray = (JArray)jItems["layout"];
                foreach (JToken jLayout in jLayoutArray)
                {
                    JObject jTemplate = (JObject)jLayout["template"];
                    var getLabel = (String)jTemplate["label"];
                    if (label == getLabel)
                    {
                        jLayout["data"] = Value;
                    }
                }
            }
            return JsonConvert.SerializeObject(jsonAdvanceForm);
        }

        public static string ReplaceDataControl(string advanceForm, string Value, string label)
        {
            JObject jsonAdvanceForm = JObject.Parse(advanceForm);
            JArray itemsArray = (JArray)jsonAdvanceForm["items"];
            foreach (JObject jItems in itemsArray)
            {
                JArray jLayoutArray = (JArray)jItems["layout"];
                foreach (JToken jLayout in jLayoutArray)
                {
                    JObject jTemplate = (JObject)jLayout["template"];
                    var getLabel = (String)jTemplate["label"];
                    if (label == getLabel)
                    {
                        JObject jData = (JObject)jLayout["data"];
                        if (jData != null)
                        {
                            jData["value"] = Value;
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(jsonAdvanceForm);
        }

        public static string GetValueInTable(Data table, int rowIndex, int colIndex)
        {
            return table.row[rowIndex][colIndex].value;
        }
    }
}
