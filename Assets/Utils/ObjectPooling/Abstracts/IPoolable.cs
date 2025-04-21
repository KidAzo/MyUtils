namespace Utils.Pooling
{
	public interface IPoolable
	{
		void Get();
		void Release();
	}
}
