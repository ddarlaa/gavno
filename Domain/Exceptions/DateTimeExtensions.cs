namespace IceBreakerApp.Domain.Exceptions
{
    /// <summary>
    /// Методы расширения для работы с DateTime в PostgreSQL
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Создает UTC DateTime для безопасной работы с PostgreSQL
        /// </summary>
        /// <param name="dateTime">Исходное время</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime ToPostgreSafeUtc(this DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _ => DateTime.UtcNow
            };
        }

        /// <summary>
        /// Создает UTC DateTime для безопасной работы с PostgreSQL (nullable версия)
        /// </summary>
        /// <param name="dateTime">Исходное время</param>
        /// <returns>UTC DateTime или null</returns>
        public static DateTime? ToPostgreSafeUtc(this DateTime? dateTime)
        {
            return dateTime?.ToPostgreSafeUtc();
        }
    }
}