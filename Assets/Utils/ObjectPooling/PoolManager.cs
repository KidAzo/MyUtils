﻿using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using Utils.Pooling.Utils.Pooling;

namespace Utils.Pooling
{
	public static class PoolManager
	{
		private static readonly Dictionary<Type, object> _pools = new();

		static PoolManager()
		{
#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
		}

#if UNITY_EDITOR
		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				ClearAll();
			}
		}
#endif

		public static void Register<T>(
			Func<T> createFunc,
			Action<T> onGet = null,
			Action<T> onRelease = null,
			Action<T> onDestroy = null,
			int defaultCapacity = 10,
			int? maxSize = null
		) where T : IPoolable
		{
			Type type = typeof(T);

			if (_pools.ContainsKey(type))
				throw new InvalidOperationException($"Pool for type {type.Name} is already registered!");

			var pool = new ObjectPool<T>(
				createFunc,
				onGet,
				onRelease,
				onDestroy,
				defaultCapacity,
				maxSize
			);

			_pools[type] = pool;
		}

		public static T Get<T>() where T : IPoolable
		{
			if (!_pools.TryGetValue(typeof(T), out var pool))
				throw new InvalidOperationException($"No pool registered for type {typeof(T).Name}");

			return ((IObjectPool<T>)pool).Get();
		}

		public static void Return<T>(T item) where T : IPoolable
		{
			if (!_pools.TryGetValue(typeof(T), out var pool))
				throw new InvalidOperationException($"No pool registered for type {typeof(T).Name}");

			((IObjectPool<T>)pool).ReturnToPool(item);
		}

		public static void Clear<T>() where T : IPoolable
		{
			if (_pools.TryGetValue(typeof(T), out var pool))
			{
				((ObjectPool<T>)pool).Clear(true);
				_pools.Remove(typeof(T));
			}
		}

		public static void ClearAll()
		{
			foreach (var pool in _pools.Values)
			{
				if (pool is IClearable clearable)
					clearable.Clear(true);
			}

			_pools.Clear();

			if (PoolingRootManager.root != null)
			{
#if UNITY_EDITOR
				if (!EditorApplication.isPlaying)
					GameObject.DestroyImmediate(PoolingRootManager.root);
				else
					GameObject.Destroy(PoolingRootManager.root);
#else
                GameObject.Destroy(PoolingRootManager.Root);
#endif

				PoolingRootManager.SubParents.Clear();
			}
		}
	}
}

