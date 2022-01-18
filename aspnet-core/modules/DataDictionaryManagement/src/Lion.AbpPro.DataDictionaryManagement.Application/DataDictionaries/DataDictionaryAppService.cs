using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lion.AbpPro.DataDictionaryManagement.DataDictionaries.Aggregates;
using Lion.AbpPro.DataDictionaryManagement.DataDictionaries.Dtos;
using Lion.AbpPro.DataDictionaryManagement.Permissions;
using Lion.AbpPro.Extension.System;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;

namespace Lion.AbpPro.DataDictionaryManagement.DataDictionaries
{
    [Authorize(DataDictionaryManagementPermissions.DataDictionaryManagement.Default)]
    public class DataDictionaryAppService : DataDictionaryManagementAppService, IDataDictionaryAppService
    {
        /// <summary>
        ///  注意 为了快速直接注入仓库层 规范上是不允许的
        ///  这里注入仓储也只是为了查询分页
        ///  如果是其他的操作全部通过对应manger进行操作
        /// </summary>
        private readonly IDataDictionaryRepository _dataDictionaryRepository;

        private readonly DataDictionaryManager _dataDictionaryManager;

        public DataDictionaryAppService(
            IDataDictionaryRepository dataDictionaryRepository,
            DataDictionaryManager dataDictionaryManager)
        {
            _dataDictionaryRepository = dataDictionaryRepository;
            _dataDictionaryManager = dataDictionaryManager;
        }

        /// <summary>
        /// 分页查询字典项
        /// </summary>
        /// <param name="input"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<PagedResultDto<PagingDataDictionaryOutput>> GetPagingListAsync(
            PagingDataDictionaryInput input)
        {
            var result = new PagedResultDto<PagingDataDictionaryOutput>();
            var totalCount = await _dataDictionaryRepository.GetPagingCountAsync(input.Filter);
            result.TotalCount = totalCount;
            if (totalCount <= 0) return result;

            var entities = await _dataDictionaryRepository.GetPagingListAsync(input.Filter, input.PageSize,
                input.SkipCount, false);
            result.Items = ObjectMapper.Map<List<DataDictionary>, List<PagingDataDictionaryOutput>>(entities);

            return result;
        }


        /// <summary>
        /// 分页查询字典项明细
        /// </summary>
        /// <param name="input"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<PagedResultDto<PagingDataDictionaryDetailOutput>> GetPagingDetailListAsync(
            PagingDataDictionaryDetailInput input)
        {
            var entity = await _dataDictionaryRepository.FindByIdAsync(input.DataDictionaryId, true);
            var details = entity.Details
                .WhereIf(input.Filter.IsNotNullOrWhiteSpace(), e => (e.Code.Contains(input.Filter) || e.DisplayText.Contains(input.Filter)))
                .OrderBy(e => e.Order)
                .Take(input.PageSize).Skip(input.SkipCount).ToList();
            return new PagedResultDto<PagingDataDictionaryDetailOutput>(
                entity.Details.Count,
                ObjectMapper.Map<List<DataDictionaryDetail>, List<PagingDataDictionaryDetailOutput>>(details));
        }


        /// <summary>
        /// 创建字典类型
        /// </summary>
        /// <returns></returns>
        [Authorize(DataDictionaryManagementPermissions.DataDictionaryManagement.Create)]
        public Task CreateAsync(CreateDataDictinaryInput input)
        {
            return _dataDictionaryManager.CreateAsync(input.Code, input.DisplayText, input.Description);
        }

        /// <summary>
        /// 新增字典明细
        /// </summary>
        [Authorize(DataDictionaryManagementPermissions.DataDictionaryManagement.Create)]
        public Task CreateDetailAsync(CreateDataDictinaryDetailInput input)
        {
            return _dataDictionaryManager.CreateDetailAsync(input.Id, input.Code, input.DisplayText, input.Description,
                input.Order);
        }

        /// <summary>
        /// 设置字典明细状态
        /// </summary>
        [Authorize(DataDictionaryManagementPermissions.DataDictionaryManagement.Update)]
        public Task SetStatus(SetDataDictinaryDetailInput input)
        {
            return _dataDictionaryManager.SetStatus(input.DataDictionaryId, input.DataDictionayDetailId,
                input.IsEnabled);
        }

        [Authorize(DataDictionaryManagementPermissions.DataDictionaryManagement.Update)]
        public Task UpdateDetailAsync(UpdateDetailInput input)
        {
            return _dataDictionaryManager.UpdateDetailAsync(input.DataDictionaryId, input.Id, input.DisplayText, input.Description,
                input.Order);
        }

        public Task DeleteAsync(DeleteDataDictionaryDetailInput input)
        {
            return _dataDictionaryManager.DeleteAsync(input.DataDictionaryId, input.DataDictionayDetailId);
        }
    }
}