using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using raceanalyst.Data.Models;

namespace raceanalyst
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<LineCrossing> LineCrossings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (null == modelBuilder)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LineCrossing>().ToTable("LineCrossings");
        }
    }
}
