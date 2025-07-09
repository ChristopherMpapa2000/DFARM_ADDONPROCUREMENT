using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AddonProcurement.Helpers;
using AddonProcurement.Managers;
using AddonProcurement.Models;
using AddonProcurement.Models.RequestModel;
using WolfApprove.Model.CustomClass;
using static AddonProcurement.Helpers.WriteLogFile;
using WolfApprove.API2.Controllers.Bean;

namespace AddonProject.Handlers
{
    public class AddonApiVersionRedirectHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri.AbsolutePath.Contains("/api/Memo/MemoDetail/MAdvancveFormByMemoIds"))
            {

                var content = await request.Content.ReadAsStringAsync();
                var requestBody = JsonConvert.DeserializeObject<RequestMadvanceForm>(content);
                var result = AddonManager.MAdvancveFormByMemoIds(requestBody);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")
                };

                return response;
            }
            if (request.RequestUri.AbsolutePath.Contains("api/services/submitform"))
            {
                var requestContent = await request.Content.ReadAsStringAsync();
                var requestModel = JsonConvert.DeserializeObject<AddonFormModel.Form_Model>(requestContent);
                var resultFromMainService = await base.SendAsync(request, cancellationToken);
                if (resultFromMainService.IsSuccessStatusCode)
                {
                    // response From Standard Process
                    var responseString = await resultFromMainService.Content.ReadAsStringAsync();
                    var responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                    requestModel.memoPage.memoDetail.memoid = responseBean.memoid;
                    AddonManager.MAdvancveFormByMemoIds_Receive(requestModel);
                }
                return resultFromMainService;
            }
            else if (request.RequestUri.AbsolutePath.Contains("/api/Memo/GetAllReferenceDoc"))
            {
                try
                {
                    WriteLogFile.LogAddon($"Start Extension Addon GetAllReferenceDoc");
                    var requestContent = await request.Content.ReadAsStringAsync();
                    var requestModel = JsonConvert.DeserializeObject<AddonFormModel.MemoDetail>(requestContent);
                    var response = await base.SendAsync(request, cancellationToken);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responeModel = JsonConvert.DeserializeObject<List<RefModel>>(responseContent);
                    var result = AddonManager.Filter_E_Procurement_ReferenceDoc(responeModel, requestModel);
                    var modifiedResponseContent = JsonConvert.SerializeObject(result);
                    response.Content = new StringContent(modifiedResponseContent, Encoding.UTF8, "application/json");
                    WriteLogFile.LogAddon($"End Extension Addon GetAllReferenceDoc");
                    return response;
                }
                catch (Exception ex)
                {
                    LogAddon($"ex : " + ex);
                }
            }
            //else if (request.RequestUri.AbsolutePath.Contains("/api/services/ValidateSubmitForm") || request.RequestUri.AbsolutePath.Contains("/api/services/submitform"))
            //{
            //    LogAddon($"Extension Addon ValidateSubmitForm");
            //    try
            //    {
            //        var requestContent = await request.Content.ReadAsStringAsync();
            //        var requestModel = JsonConvert.DeserializeObject<AddonFormModel.Form_Model>(requestContent);
            //        var resultIsDuplicateInMemo = AddonManager.IsDuplicateInMemo(requestModel.memoPage);
            //        var resultDuplicateProductId = AddonManager.DuplicateProductId(requestModel.memoPage);

            //        LogAddon($"Extension Addon resultIsDuplicateInMemo result : {resultIsDuplicateInMemo}");
            //        LogAddon($"Extension Addon resultDuplicateProductId result : {resultDuplicateProductId}");

            //        if (!string.IsNullOrEmpty(resultIsDuplicateInMemo))
            //        {
            //            var errorResponse = new
            //            {
            //                Message = resultIsDuplicateInMemo
            //            };
            //            var response = new HttpResponseMessage(HttpStatusCode.OK)
            //            {
            //                Content = new StringContent(JsonConvert.SerializeObject(errorResponse), System.Text.Encoding.UTF8, "application/json")
            //            };

            //            return response;
            //        }
            //        if (!string.IsNullOrEmpty(resultDuplicateProductId))
            //        {
            //            var errorResponse = new
            //            {
            //                Message = resultDuplicateProductId
            //            };
            //            var response = new HttpResponseMessage(HttpStatusCode.OK)
            //            {
            //                Content = new StringContent(JsonConvert.SerializeObject(errorResponse), System.Text.Encoding.UTF8, "application/json")
            //            };

            //            return response;
            //        }
            //        return await base.SendAsync(request, cancellationToken);
            //    }
            //    catch (Exception ex)
            //    {
            //        LogAddon($"Extension Addon Error : {ex}");
            //        throw ex;
            //    }
            //}

            return await base.SendAsync(request, cancellationToken);
        }

        public HttpRequestMessage CloneAndModifyHttpRequest(HttpRequestMessage request, string oldPath, string newPath)
        {
            var newRequest = new HttpRequestMessage(request.Method,
                new Uri(request.RequestUri.AbsoluteUri
                .Replace(oldPath, newPath)))
            {
                Content = request.Content,
                Version = request.Version
            };

            foreach (var header in request.Headers)
            {
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return newRequest;
        }
    }
}