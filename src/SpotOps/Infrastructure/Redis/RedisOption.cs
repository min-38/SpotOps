namespace SpotOps.Infrastructure.Redis;

public sealed class RedisOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 6379;
    public string Password { get; init; } = "";

    // Redis database index.
    public int Db { get; init; } = 0;
    
    // Redis 키 접두사. 여러 애플리케이션이 같은 Redis 인스턴스를 사용할 때 키 충돌 방지용.
    public string KeyPrefix { get; init; } = "spotops";

    // Redis 연결 기본 TTL(Time To Live, 만료 시간) 설정
    public int DefaultTtlHours { get; init; } = 24;
}
