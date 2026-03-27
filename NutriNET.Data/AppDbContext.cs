namespace NutriNET.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<CommentRestriction> CommentRestrictions { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<Food> Foods { get; set; }
        public DbSet<FoodRequest> FoodRequests { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<MealFood> MealFoods { get; set; }
        public DbSet<ModeratorRequest> ModeratorRequests { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeComment> RecipeComments { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<RecipeList> RecipeLists { get; set; }
        public DbSet<RecipeListItem> RecipeListItems { get; set; }
        public DbSet<RecipeRating> RecipeRatings { get; set; }
        public DbSet<User> Users { get; set; }

        public AppDbContext() { }

        public AppDbContext(DbContextOptions options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySQL();
            }
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommentRestriction>(entity =>
            {
                entity.HasOne(cr => cr.User)
                      .WithMany(u => u.CommentRestrictions)
                      .HasForeignKey(cr => cr.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Follower>(entity =>
            {
                entity.HasOne(f => f.FollowerUser)
                      .WithMany(u => u.Following)
                      .HasForeignKey(f => f.FollowerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.FollowingUser)
                      .WithMany(u => u.Followers)
                      .HasForeignKey(f => f.FollowingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Food>(entity =>
            { 
                entity.HasMany<RecipeIngredient>()
                      .WithOne(ri => ri.Food)
                      .HasForeignKey(ri => ri.FoodId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany<MealFood>()
                      .WithOne(mf => mf.Food)
                      .HasForeignKey(mf => mf.FoodId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FoodRequest>(entity =>
            {
                entity.HasOne(fr => fr.Sender)
                      .WithMany(u=>u.SentFoodRequests)
                      .HasForeignKey(fr => fr.SenderId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(fr => fr.ActionedBy)
                      .WithMany(u=>u.ActionedFoodRequests)
                      .HasForeignKey(fr => fr.ActionedById)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Meal>(entity =>
            {
                entity.HasOne(m => m.User)
                      .WithMany(u => u.Meals)
                      .HasForeignKey(m => m.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(m => m.MealFoods)
                      .WithOne(mf => mf.Meal)
                      .HasForeignKey(mf => mf.MealId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MealFood>(entity =>
            {
                entity.HasOne(mf => mf.Meal)
                      .WithMany(m => m.MealFoods)
                      .HasForeignKey(mf => mf.MealId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mf => mf.Recipe)
                     .WithMany(r => r.MealFoods)
                     .HasForeignKey(mf => mf.RecipeId)
                     .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ModeratorRequest>(entity =>
            {
                entity.Property(mr => mr.RequestDescription)
                      .HasMaxLength(500)
                      .IsRequired();

                entity.HasOne(mr => mr.Sender)
                    .WithMany(u => u.SentModeratorRequests)
                    .HasForeignKey(mr => mr.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mr => mr.ActionedBy)
                    .WithMany(u => u.ActionedModeratorRequests)
                    .HasForeignKey(mr => mr.ActionedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.Property(r => r.Name)
                      .HasMaxLength(80)
                      .IsRequired();

                entity.Property(r => r.Description)
                      .HasMaxLength(2000)
                      .IsRequired();

                entity.HasOne(r => r.Creator)
                      .WithMany(c=>c.CreatedRecipes)
                      .HasForeignKey(r => r.CreatorId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(r => r.Ingredients)
                      .WithOne(ri => ri.Recipe)
                      .HasForeignKey(ri => ri.RecipeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(r => r.Comments)
                      .WithOne(c => c.Recipe)
                      .HasForeignKey(c => c.RecipeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(r => r.RecipeRatings)
                      .WithOne(rr => rr.Recipe)
                      .HasForeignKey(rr => rr.RecipeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecipeComment>(entity =>
            {
                entity.Property(rc => rc.Comment)
                      .HasMaxLength(500)
                      .IsRequired();

                entity.HasOne(rc => rc.User)
                      .WithMany(u=>u.RecipeComments)
                      .HasForeignKey(rc => rc.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecipeIngredient>();

            modelBuilder.Entity<RecipeList>(entity =>
            {
                entity.HasOne(fg => fg.User)
                      .WithMany(u => u.RecipeLists)
                      .HasForeignKey(fg => fg.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(rl => rl.RecipeListItems)
                      .WithOne(rli => rli.RecipeList)
                      .HasForeignKey(rli=>rli.RecipeListId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecipeListItem>(entity =>
            {
                entity.HasOne(rli => rli.Recipe)
                      .WithMany(r => r.RecipeListItems)
                      .HasForeignKey(rli => rli.RecipeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RecipeRating>(entity =>
            {
                entity.HasOne(rr => rr.User)
                      .WithMany(u=>u.RecipeRatings)
                      .HasForeignKey(rr => rr.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
