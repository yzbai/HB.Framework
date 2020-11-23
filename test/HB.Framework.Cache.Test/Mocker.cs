﻿using System;
using System.Collections.Generic;

namespace HB.Framework.Cache.Test
{
    public static class Mocker
    {
        private static readonly Random _random = new Random();
        public static Book MockOne()
        {
            return new Book
            {
                Name = SecurityUtil.CreateUniqueToken(),
                BookID = DateTimeOffset.UtcNow.Ticks,
                Publisher = _random.Next().ToString(),
                Price = _random.NextDouble() * 1000
            };
        }

        internal static List<Book> MockMany()
        {
            List<Book> books = new List<Book>();

            for (int i = 0; i < 100; ++i)
            {
                books.Add(new Book
                {
                    Guid = "Guid" + i.ToString(),
                    Name = "Name" + i.ToString(),
                    BookID = i,
                    Publisher = _random.Next().ToString(),
                    Price = _random.NextDouble() * 1000
                });
            }

            return books;
        }
    }
}
