﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abp.DynamicEntityProperties
{
    public class NullDynamicPropertyValueStore : IDynamicPropertyValueStore
    {
        public static NullDynamicPropertyValueStore Instance = new NullDynamicPropertyValueStore();

        public DynamicPropertyValue Get(long id)
        {
            return default;
        }

        public Task<DynamicPropertyValue> GetAsync(long id)
        {
            return Task.FromResult<DynamicPropertyValue>(default);
        }

        public List<DynamicPropertyValue> GetAllValuesOfDynamicProperty(int dynamicPropertyId)
        {
            return new List<DynamicPropertyValue>();
        }

        public Task<List<DynamicPropertyValue>> GetAllValuesOfDynamicPropertyAsync(int dynamicPropertyId)
        {
            return Task.FromResult(new List<DynamicPropertyValue>());
        }

        public void Add(DynamicPropertyValue dynamicPropertyValue)
        {
        }

        public Task AddAsync(DynamicPropertyValue dynamicPropertyValue)
        {
            return Task.CompletedTask;
        }

        public void Update(DynamicPropertyValue dynamicPropertyValue)
        {
        }

        public Task UpdateAsync(DynamicPropertyValue dynamicPropertyValue)
        {
            return Task.CompletedTask;
        }

        public void Delete(long id)
        {
        }

        public Task DeleteAsync(long id)
        {
            return Task.CompletedTask;
        }

        public void CleanValues(int dynamicPropertyId)
        {
        }

        public Task CleanValuesAsync(int dynamicPropertyId)
        {
            return Task.CompletedTask;
        }
    }
}