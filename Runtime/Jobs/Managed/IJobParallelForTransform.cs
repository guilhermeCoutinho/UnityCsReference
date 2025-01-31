// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using static Unity.Collections.LowLevel.Unsafe.BurstLike;
using Unity.Burst;
using System.Diagnostics;

//@TODO: Move this into Runtime/Transform folder with the test of Transform component
namespace UnityEngine.Jobs
{
    [JobProducerType(typeof(IJobParallelForTransformExtensions.TransformParallelForLoopStruct<>))]
    public interface IJobParallelForTransform
    {
        void Execute(int index, TransformAccess transform);
    }

    public static class IJobParallelForTransformExtensions
    {
        internal struct TransformParallelForLoopStruct<T> where T : struct, IJobParallelForTransform
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<TransformParallelForLoopStruct<T>>();

            [BurstDiscard]
            internal static unsafe void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            [StructLayout(LayoutKind.Sequential)]
            struct TransformJobData
            {
#pragma warning disable 0649
                public IntPtr TransformAccessArray;
                public int IsReadOnly;
#pragma warning restore 0649
            }

            public delegate void ExecuteJobFunction(ref T jobData, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public static unsafe void Execute(ref T jobData, System.IntPtr jobData2, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                UnsafeUtility.CopyPtrToStructure((void*)jobData2, out TransformJobData transformJobData);

                int* sortedToUserIndex = (int*)TransformAccessArray.GetSortedToUserIndex(transformJobData.TransformAccessArray);
                TransformAccess* sortedTransformAccess = (TransformAccess*)TransformAccessArray.GetSortedTransformAccess(transformJobData.TransformAccessArray);

                if (transformJobData.IsReadOnly == 1)
                {
                    while (true)
                    {
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var begin, out var end))
                            break;

                        var endThatCompilerCanSeeWillNeverChange = end;
                        for (var i = begin; i < endThatCompilerCanSeeWillNeverChange; ++i)
                        {
                            int sortedIndex = i;
                            int userIndex = sortedToUserIndex[sortedIndex];
                            JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), userIndex, 1);
                            var transformAccess = sortedTransformAccess[sortedIndex];
                            transformAccess.MarkReadOnly();
                            jobData.Execute(userIndex, transformAccess);
                        }
                    }
                }
                else
                {
                    JobsUtility.GetJobRange(ref ranges, jobIndex, out var begin, out var end);
                    for (int i = begin; i < end; i++)
                    {
                        int sortedIndex = i;
                        int userIndex = sortedToUserIndex[sortedIndex];
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), userIndex, 1);
                        var transformAccess = sortedTransformAccess[sortedIndex];
                        transformAccess.MarkReadWrite();
                        jobData.Execute(userIndex, transformAccess);
                    }
                }
            }
        }

        public static void EarlyJobInit<T>()
            where T : struct, IJobParallelForTransform
        {
            TransformParallelForLoopStruct<T>.Initialize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReflectionDataCorrect(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Support for burst compiled calls to Schedule depends on the Jobs package.\n\nFor generic job types, please include [assembly: RegisterGenericJobType(typeof(MyJob<MyJobSpecialization>))] in your source file.");
        }

        private static IntPtr GetReflectionData<T>()
            where T : struct, IJobParallelForTransform
        {
            TransformParallelForLoopStruct<T>.Initialize();
            var reflectionData = TransformParallelForLoopStruct<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            return reflectionData;
        }

        unsafe static public JobHandle Schedule<T>(this T jobData, TransformAccessArray transforms, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForTransform
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelForTransform(ref scheduleParams, transforms.GetTransformAccessArrayForSchedule());
        }

        public static unsafe JobHandle ScheduleReadOnly<T>(this T jobData, TransformAccessArray transforms, int batchSize, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForTransform
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelForTransformReadOnly(ref scheduleParams, transforms.GetTransformAccessArrayForSchedule(), batchSize);
        }

        public static unsafe void RunReadOnly<T>(this T jobData, TransformAccessArray transforms) where T : struct, IJobParallelForTransform
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), default, ScheduleMode.Run);
            JobsUtility.ScheduleParallelForTransformReadOnly(ref scheduleParams, transforms.GetTransformAccessArrayForSchedule(), transforms.length);
        }

        //@TODO: Run
    }
}
