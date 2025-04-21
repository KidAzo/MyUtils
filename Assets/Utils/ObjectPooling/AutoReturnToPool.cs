using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

namespace Utils.Pooling
{
	public class AutoReturnToPool<T> : MonoBehaviour where T : MonoBehaviour, IPoolable
	{
		[SerializeField] private float returnDelay = 1f;
		[SerializeField] private bool autoStartOnEnable = true;

		private CancellationTokenSource _cts;

		protected virtual void OnEnable()
		{
			if (autoStartOnEnable)
				StartAutoReturn(returnDelay);
		}

		protected virtual void OnDisable()
		{
			CancelAsync();
		}

		private void CancelAsync()
		{
			if (_cts != null)
			{
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
		}

		public void StartAutoReturn(float delay)
		{
			CancelAsync();
			_cts = new CancellationTokenSource();
			DelayAndReturnAsync(delay, _cts.Token).SuppressCancellationThrow().Forget();
		}

		private async UniTaskVoid DelayAndReturnAsync(float delay, CancellationToken cancellationToken)
		{
			bool isCancelled = await UniTask.Delay(delay, cancellationToken: cancellationToken)
				.SuppressCancellationThrow();

			if (!isCancelled && this && gameObject.activeInHierarchy)
			{
				PoolManager.Return(this as T);
			}
		}
	}
}