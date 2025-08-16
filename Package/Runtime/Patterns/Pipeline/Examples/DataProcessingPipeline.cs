using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Pipeline.Examples
{
    /// <summary>
    /// Veri işleme pipeline örneği
    /// </summary>
    public class DataProcessingPipeline
    {
        /// <summary>
        /// Basit veri işleme pipeline'ı oluştur
        /// </summary>
        /// <returns>Pipeline</returns>
        public static IPipeline<List<string>, ProcessedData> CreateSimpleDataPipeline()
        {
            return PipelineFactory.CreateSequential("Simple Data Processing", "Basit veri işleme pipeline'ı")
                .AddStep<ValidateDataStep>()
                .AddStep<CleanDataStep>()
                .AddStep<TransformDataStep>()
                .AddStep<AggregateDataStep>()
                .WithTimeout((int)TimeSpan.FromMinutes(5).TotalMilliseconds)
                .Build<List<string>, ProcessedData>();
        }
        
        /// <summary>
        /// Paralel veri işleme pipeline'ı oluştur
        /// </summary>
        /// <returns>Pipeline</returns>
        public static IPipeline<List<string>, ProcessedData> CreateParallelDataPipeline()
        {
            return PipelineFactory.CreateParallel("Parallel Data Processing", "Paralel veri işleme pipeline'ı")
                .AddStep<ValidateDataStep>()
                .AddParallelStep(new CleanDataStep())
                .AddParallelStep(new TransformDataStep())
                .AddParallelStep(new EnrichDataStep())
                .AddStep<AggregateDataStep>() // Son adım sequential
                .WithMaxParallelSteps(3)
                .WithTimeout(TimeSpan.FromMinutes(3))
                .Build<List<string>, ProcessedData>();
        }
        
        /// <summary>
        /// Conditional veri işleme pipeline'ı oluştur
        /// </summary>
        /// <returns>Pipeline</returns>
        public static IPipeline<List<string>, ProcessedData> CreateConditionalDataPipeline()
        {
            return PipelineFactory.CreateConditional("Conditional Data Processing", "Koşullu veri işleme pipeline'ı")
                .AddStep<ValidateDataStep>()
                .AddConditionalStep(new CleanDataStep(), ctx => ctx.GetMetadata<bool>("needsCleaning"))
                .AddConditionalStep(new TransformDataStep(), ctx => ctx.GetMetadata<bool>("needsTransformation"))
                .AddConditionalStep(new EnrichDataStep(), async ctx => 
                {
                    var dataSize = ctx.GetData<List<string>>()?.Count ?? 0;
                    return await Task.FromResult(dataSize > 100);
                })
                .AddStep<AggregateDataStep>()
                .WithTimeout((int)TimeSpan.FromMinutes(10).TotalMilliseconds)
                .Build<List<string>, ProcessedData>();
        }
    }
    
    /// <summary>
    /// İşlenmiş veri modeli
    /// </summary>
    [Serializable]
    public class ProcessedData
    {
        public List<string> CleanedData { get; set; } = new List<string>();
        public List<string> TransformedData { get; set; } = new List<string>();
        public Dictionary<string, object> EnrichedData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, int> AggregatedData { get; set; } = new Dictionary<string, int>();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ProcessingTime { get; set; }
        public int TotalRecords { get; set; }
        public int ValidRecords { get; set; }
        public int InvalidRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Veri validation step
    /// </summary>
    public class ValidateDataStep : BasePipelineStep<List<string>, List<string>>
    {
        public ValidateDataStep() : base("Validate Data", "Gelen veriyi validate eder", 100)
        {
            MaxRetries = 2;
            TimeoutMs = 30000;
        }
        
        protected override async Task<(List<string> Output, PipelineStepResult Result)> OnExecuteAsync(List<string> input, PipelineContext context)
        {
            if (input == null)
            {
                context.SetError("Input data is null");
                return (null, PipelineStepResult.Failed);
            }
            
            var validData = new List<string>();
            var invalidCount = 0;
            
            foreach (var item in input)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    invalidCount++;
                    continue;
                }
                
                if (item.Length < 3)
                {
                    invalidCount++;
                    continue;
                }
                
                validData.Add(item.Trim());
            }
            
            // Metadata'ya validation sonuçlarını ekle
            context.SetMetadata("totalRecords", input.Count);
            context.SetMetadata("validRecords", validData.Count);
            context.SetMetadata("invalidRecords", invalidCount);
            context.SetMetadata("needsCleaning", validData.Any(x => x.Contains("  ")));
            context.SetMetadata("needsTransformation", validData.Any(x => x.Any(char.IsUpper)));
            
            context.SetData(validData);
            
            Debug.Log($"[ValidateDataStep] Validated {validData.Count}/{input.Count} records");
            
            await Task.Delay(100, context.CancellationToken); // Simulate processing time
            return (validData, PipelineStepResult.Success);
        }
    }
    
    /// <summary>
    /// Veri temizleme step
    /// </summary>
    public class CleanDataStep : BasePipelineStep<List<string>, List<string>>
    {
        public CleanDataStep() : base("Clean Data", "Veriyi temizler ve normalize eder", 90)
        {
            MaxRetries = 1;
            TimeoutMs = 60000;
        }
        
        protected override async Task<(List<string> Output, PipelineStepResult Result)> OnExecuteAsync(List<string> input, PipelineContext context)
        {
            if (input == null || !input.Any())
            {
                return (new List<string>(), PipelineStepResult.Skip);
            }
            
            var cleanedData = new List<string>();
            
            foreach (var item in input)
            {
                var cleaned = item
                    .Trim()
                    .Replace("  ", " ") // Çift boşlukları tek boşluğa çevir
                    .Replace("\t", " ") // Tab'ları boşluğa çevir
                    .Replace("\n", " ") // Yeni satırları boşluğa çevir
                    .Replace("\r", ""); // Carriage return'leri kaldır
                
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    cleanedData.Add(cleaned);
                }
            }
            
            context.SetData(cleanedData);
            
            Debug.Log($"[CleanDataStep] Cleaned {cleanedData.Count} records");
            
            await Task.Delay(200, context.CancellationToken); // Simulate processing time
            return (cleanedData, PipelineStepResult.Success);
        }
    }
    
    /// <summary>
    /// Veri transformation step
    /// </summary>
    public class TransformDataStep : BasePipelineStep<List<string>, List<string>>
    {
        public TransformDataStep() : base("Transform Data", "Veriyi dönüştürür", 80)
        {
            MaxRetries = 1;
            TimeoutMs = 60000;
        }
        
        protected override async Task<(List<string> Output, PipelineStepResult Result)> OnExecuteAsync(List<string> input, PipelineContext context)
        {
            if (input == null || !input.Any())
            {
                return (new List<string>(), PipelineStepResult.Skip);
            }
            
            var transformedData = new List<string>();
            
            foreach (var item in input)
            {
                var transformed = item.ToLowerInvariant(); // Küçük harfe çevir
                
                // Özel karakterleri kaldır
                transformed = System.Text.RegularExpressions.Regex.Replace(transformed, @"[^a-z0-9\s]", "");
                
                if (!string.IsNullOrWhiteSpace(transformed))
                {
                    transformedData.Add(transformed);
                }
            }
            
            Debug.Log($"[TransformDataStep] Transformed {transformedData.Count} records");
            
            await Task.Delay(300, context.CancellationToken); // Simulate processing time
            return (transformedData, PipelineStepResult.Success);
        }
    }
    
    /// <summary>
    /// Veri enrichment step
    /// </summary>
    public class EnrichDataStep : BasePipelineStep<List<string>, List<string>>
    {
        public EnrichDataStep() : base("Enrich Data", "Veriyi zenginleştirir", 70)
        {
            MaxRetries = 1;
            TimeoutMs = 120000;
        }
        
        protected override async Task<(List<string>, PipelineStepResult)> OnExecuteAsync(List<string> input, PipelineContext context)
        {
            var enrichedData = new List<string>();
            
            foreach (var item in input)
            {
                // Veriyi zenginleştir
                var enrichedItem = $"[ENRICHED-{DateTime.Now:HH:mm:ss}] {item.ToUpper()}";
                enrichedData.Add(enrichedItem);
                
                // Simüle edilmiş async işlem
                await Task.Delay(10);
            }
            
            context.SetMetadata("enriched_count", enrichedData.Count);
            return (enrichedData, PipelineStepResult.Success);
        }
    }
    
    /// <summary>
    /// Veri aggregation step
    /// </summary>
    public class AggregateDataStep : BasePipelineStep<List<string>, ProcessedData>
    {
        public AggregateDataStep() : base("Aggregate Data", "Veriyi toplar ve final sonucu oluşturur", 60)
        {
            MaxRetries = 1;
            TimeoutMs = 30000;
        }
        
        protected override async Task<(ProcessedData Output, PipelineStepResult Result)> OnExecuteAsync(List<string> input, PipelineContext context)
        {
            if (input == null)
            {
                input = new List<string>();
            }
            
            var result = new ProcessedData
            {
                CleanedData = input,
                TransformedData = input,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - context.StartTime,
                TotalRecords = context.GetMetadata<int>("totalRecords"),
                ValidRecords = context.GetMetadata<int>("validRecords"),
                InvalidRecords = context.GetMetadata<int>("invalidRecords")
            };
            
            // Enriched data'yı al
            var enrichedData = context.GetMetadata<Dictionary<string, object>>("enrichedData");
            if (enrichedData != null)
            {
                result.EnrichedData = enrichedData;
                
                if (enrichedData.ContainsKey("wordCounts"))
                {
                    result.AggregatedData = enrichedData["wordCounts"] as Dictionary<string, int> ?? new Dictionary<string, int>();
                }
            }
            
            // Hataları topla
            if (context.HasError)
            {
                result.Errors.Add(context.Error);
            }
            
            context.SetData(result);
            
            Debug.Log($"[AggregateDataStep] Aggregated {result.ValidRecords} valid records from {result.TotalRecords} total records");
            
            await Task.Delay(100, context.CancellationToken); // Simulate processing time
            return (result, PipelineStepResult.Success);
        }
    }
}
