using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Интерфейс
{
    public static class PasswordHasher
    {
        // Конфигурация для паролей
        private const int SaltSizeInBits = 128;
        private const int HashSizeInBits = 256;
        private const int Iterations = 100000;

        private static int SaltSizeInBytes => SaltSizeInBits / 8;
        private static int HashSizeInBytes => HashSizeInBits / 8;

        /// <summary>
        /// Создает хеш пароля с солью
        /// </summary>
        public static string HashDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return null;

            // Для устройств используем SHA256 без соли, так как нужно сравнивать
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(deviceId);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Проверяет, соответствует ли устройство сохраненному хэшу
        /// </summary>
        public static bool VerifyDevice(string deviceId, string storedHash)
        {
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(storedHash))
                return false;

            string computedHash = HashDevice(deviceId);
            return computedHash == storedHash;
        }

        /// <summary>
        /// Переводит пароль в хэш
        /// </summary>
        public static string HashPassword(string password)
        {
            // 1. Генерируем случайную соль
            byte[] salt = GenerateSalt(SaltSizeInBytes);

            // 2. Хешируем пароль с помощью PBKDF2
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSizeInBytes
            );

            // 3. Объединяем соль и хеш в одну строку
            byte[] combinedBytes = new byte[SaltSizeInBytes + HashSizeInBytes];
            Buffer.BlockCopy(salt, 0, combinedBytes, 0, SaltSizeInBytes);
            Buffer.BlockCopy(hash, 0, combinedBytes, SaltSizeInBytes, HashSizeInBytes);

            // 4. Конвертируем в Base64 для хранения в БД
            return Convert.ToBase64String(combinedBytes);
        }

        /// <summary>
        /// Проверяет пароль с сохраненным хешем
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                if (string.IsNullOrEmpty(storedHash))
                {
                    Debug.WriteLine("Stored hash is empty!");
                    return false;
                }

                byte[] combinedBytes = Convert.FromBase64String(storedHash);

                byte[] salt = new byte[SaltSizeInBytes];
                Buffer.BlockCopy(combinedBytes, 0, salt, 0, SaltSizeInBytes);

                byte[] storedHashBytes = new byte[HashSizeInBytes];
                Buffer.BlockCopy(combinedBytes, SaltSizeInBytes, storedHashBytes, 0, HashSizeInBytes);

                byte[] computedHash = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: Iterations,
                    numBytesRequested: HashSizeInBytes
                );

                return ByteArraysEqual(storedHashBytes, computedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Генерирует криптографически случайную соль
        /// </summary>
        private static byte[] GenerateSalt(int size)
        {
            byte[] salt = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Получает информацию о хеше (для отладки)
        /// </summary>
        public static string GetHashInfo(string storedHash)
        {
            try
            {
                byte[] combinedBytes = Convert.FromBase64String(storedHash);
                return $"Total size: {combinedBytes.Length * 8} bits\n" +
                       $"Salt size: {SaltSizeInBytes * 8} bits ({SaltSizeInBytes} bytes)\n" +
                       $"Hash size: {HashSizeInBytes * 8} bits ({HashSizeInBytes} bytes)";
            }
            catch
            {
                return "Invalid hash format";
            }
        }

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            var areSame = true;
            for (int i = 0; i < a.Length; i++)
            {
                areSame &= (a[i] == b[i]); // Проверяем равенство каждого байта
            }
            return areSame;
        }
    }
}
