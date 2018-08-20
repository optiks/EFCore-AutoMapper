using AutoMapper;
using AutoMapper.EquivalencyExpression;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void CanUpdate()
        {
            // Arrange
            var databaseName = Guid.NewGuid().ToString();

            var z1 = new Z { Name = "Initial X1-Y1-Z1" };
            var z2 = new Z { Name = "Initial X1-Y1-Z2", };
            var y1 = new Y { Name = "Initial X1-Y1", ZList = new List<Z> { z1, z2 } };
            var x1 = new X { Name = "Initial X1", YList = new List<Y>() { y1 } };
            var dbContext = GetMyDbContext(databaseName);
            dbContext.Add(x1);
            dbContext.SaveChanges();

            var x1Detached = new X
            {
                Id = x1.Id,
                Name = "Initial X1 (Updated)",

                YList = new List<Y>()
                {
                    new Y
                    {
                        Id = y1.Id,
                        XId = y1.XId,
                        Name = "Initial X1-Y1 (Updated)",

                        ZList = new List<Z>
                        {
                            new Z
                            {
                                Id = z2.Id,
                                YId = z2.YId,
                                Name = "Initial X1-Y1-Z1 (Updated)",
                            },
                            new Z
                            {
                                Name = "Initial X1-Y1-Z1 (Inserted)",
                            }
                        }
                    }
                }
            };

            Mapper.Initialize(cfg =>
            {
                cfg.AddCollectionMappers();

                cfg.CreateMap<X, X>().EqualityComparison((source, dest) => source.Id == dest.Id);
                cfg.CreateMap<Y, Y>().EqualityComparison((source, dest) => source.Id == dest.Id);
                cfg.CreateMap<Z, Z>().EqualityComparison((source, dest) => source.Id == dest.Id);
            });

            // Act
            var dbContext2 = GetMyDbContext(databaseName);
            var x1FromDb =
                dbContext2.X
                           .Include(x => x.YList)
                              .ThenInclude(x => x.ZList)
                           .SingleOrDefault(x => x.Id == x1.Id);
            Mapper.Map(x1Detached, x1FromDb);
            dbContext2.SaveChanges();

            // Assert
            Assert.Equal(expected: "Initial X1 (Updated)", actual: x1FromDb.Name);
            Assert.Equal(expected: "Initial X1-Y1 (Updated)", actual: x1FromDb.YList.Single().Name);
            Assert.Equal(expected: 2, actual: x1FromDb.YList.Single().ZList.Count);
            Assert.Equal(expected: "Initial X1-Y1-Z1 (Updated)", actual: x1FromDb.YList.Single().ZList.Single(x => x.Id == z2.Id).Name);
            Assert.Equal(expected: "Initial X1-Y1-Z1 (Inserted)", actual: x1FromDb.YList.Single().ZList.Single(x => x.Id != z2.Id).Name);
        }

        private MyDbContext GetMyDbContext(string databaseName)
        {
            var options =
                new DbContextOptionsBuilder<MyDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .EnableSensitiveDataLogging()
                    .Options;

            var dbContext = new MyDbContext(options);

            return dbContext;
        }
    }

    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        public DbSet<X> X { get; set; }
        public DbSet<Y> Y { get; set; }
        public DbSet<Z> Z { get; set; }
    }

    [DebuggerDisplay("Id={Id}, Name={Name}")]
    public class X
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Y> YList { get; set; }

        [NotMapped]
        public Guid Uid { get; } = Guid.NewGuid();
    }

    [DebuggerDisplay("Id={Id}, Yid={XId}, Name={Name}")]
    public class Y
    {
        public int Id { get; set; }
        public int XId { get; set; }
        public string Name { get; set; }

        public ICollection<Z> ZList { get; set; }

        [NotMapped]
        public Guid Uid { get; } = Guid.NewGuid();
    }

    [DebuggerDisplay("Id={Id}, Yid={YId}, Name={Name}")]
    public class Z
    {
        public int Id { get; set; }
        public int YId { get; set; }
        public string Name { get; set; }

        [NotMapped]
        public Guid Uid { get; } = Guid.NewGuid();
    }
}
