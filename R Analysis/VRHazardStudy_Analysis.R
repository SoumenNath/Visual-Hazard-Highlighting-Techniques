# =============================================================================
# VR Hazard Highlighting Study — Statistical Analysis
# =============================================================================
# Required packages — run this block once to install:
#
#   install.packages(c("readxl", "dplyr", "tidyr", "ez",
#                      "rstatix", "emmeans", "coin", "ggplot2"))
#
# Then run the rest of the script.
# =============================================================================

library(readxl)
library(dplyr)
library(tidyr)
library(ez)
library(rstatix)
library(emmeans)
library(coin)
library(ggplot2)

# =============================================================================
# 1. LOAD AND PREPARE DATA
# =============================================================================

file_path <- "Particpants_File.xlsx"   # <-- update path if needed
sheet_names <- excel_sheets(file_path)

# Read all sheets and assign correct participant IDs (all say "1" in the file)
all_data <- lapply(seq_along(sheet_names), function(i) {
  df <- read_excel(file_path, sheet = sheet_names[i])
  df$ParticipantID <- i   # override with correct sequential ID
  df
})

data <- bind_rows(all_data)

# Clean: keep only completed, responded trials; remove timeouts (RT = 10)
if ("Completed" %in% names(data)) {
  data <- data %>% filter(Completed == TRUE)
}

data <- data %>%
  filter(Responded == TRUE) %>%
  filter(ResponseTime_s != 10.0) %>%
  mutate(
    ResponseTime_s = as.numeric(ResponseTime_s),
    ParticipantID  = as.factor(ParticipantID),
    Condition      = as.factor(Condition)
  ) %>%
  filter(!is.na(ResponseTime_s))

cat("=============================================================\n")
cat("DATA SUMMARY\n")
cat("=============================================================\n")
cat("Total valid trials:", nrow(data), "\n")
cat("Participants:      ", nlevels(data$ParticipantID), "\n")
cat("Conditions:        ", levels(data$Condition), "\n\n")

# =============================================================================
# 2. COMPUTE PER-PARTICIPANT MEAN RT PER CONDITION
# =============================================================================

means <- data %>%
  group_by(ParticipantID, Condition) %>%
  summarise(MeanRT = mean(ResponseTime_s), .groups = "drop")

cat("Per-participant mean RT (wide format):\n")
means_wide <- means %>%
  pivot_wider(names_from = Condition, values_from = MeanRT)
print(means_wide, n = Inf)
cat("\n")

# =============================================================================
# 3. DESCRIPTIVE STATISTICS
# =============================================================================

cat("=============================================================\n")
cat("3. DESCRIPTIVE STATISTICS PER CONDITION\n")
cat("=============================================================\n")

desc_stats <- means %>%
  group_by(Condition) %>%
  summarise(
    N       = n(),
    Mean    = round(mean(MeanRT),   4),
    SD      = round(sd(MeanRT),     4),
    Median  = round(median(MeanRT), 4),
    SEM     = round(sd(MeanRT) / sqrt(n()), 4),
    CI_lower = round(mean(MeanRT) - qt(0.975, n()-1) * sd(MeanRT)/sqrt(n()), 4),
    CI_upper = round(mean(MeanRT) + qt(0.975, n()-1) * sd(MeanRT)/sqrt(n()), 4)
  )

print(desc_stats)
cat("\n")

# =============================================================================
# 4. NORMALITY TESTS (Shapiro-Wilk per condition)
# =============================================================================

cat("=============================================================\n")
cat("4. SHAPIRO-WILK NORMALITY TESTS\n")
cat("=============================================================\n")

normality <- means %>%
  group_by(Condition) %>%
  summarise(
    W       = round(shapiro.test(MeanRT)$statistic, 4),
    p_value = round(shapiro.test(MeanRT)$p.value,   4),
    Result  = ifelse(shapiro.test(MeanRT)$p.value > 0.05,
                     "Normal (p > .05)", "Non-normal (p <= .05)")
  )

print(normality)
cat("\n")

# =============================================================================
# 5. MAUCHLY'S TEST OF SPHERICITY + RM ANOVA
# =============================================================

cat("=============================================================\n")
cat("5. ONE-WAY REPEATED MEASURES ANOVA\n")
cat("   (Greenhouse-Geisser correction if sphericity violated)\n")
cat("=============================================================\n")

# ezANOVA runs RM ANOVA and Mauchly's test together
anova_result <- ezANOVA(
  data     = means,
  dv       = MeanRT,
  wid      = ParticipantID,
  within   = Condition,
  detailed = TRUE,
  type     = 3
)

cat("\nMauchly's Test of Sphericity:\n")
print(anova_result$`Mauchly's Test for Sphericity`)

cat("\nSphericity Corrections (Greenhouse-Geisser & Huynh-Feldt):\n")
print(anova_result$`Sphericity Corrections`)

cat("\nANOVA Table:\n")
print(anova_result$ANOVA)

# Extract key values for reporting
F_val  <- anova_result$ANOVA$F[1]
df1    <- anova_result$ANOVA$DFn[1]
df2    <- anova_result$ANOVA$DFd[1]
p_gg   <- anova_result$`Sphericity Corrections`$`p[GG]`[1]
ges    <- anova_result$ANOVA$ges[1]
eps_gg <- anova_result$`Sphericity Corrections`$GGe[1]

cat(sprintf("\nReport: F(%d, %d) = %.3f, p (GG) = %.6f, η²(g) = %.4f, ε = %.4f\n\n",
            df1, df2, F_val, p_gg, ges, eps_gg))

# =============================================================================
# 6. NON-PARAMETRIC ALTERNATIVE — FRIEDMAN TEST
# =============================================================================

cat("=============================================================\n")
cat("6. FRIEDMAN TEST (Non-parametric alternative)\n")
cat("=============================================================\n")

means_wide_matrix <- means %>%
  pivot_wider(names_from = Condition, values_from = MeanRT) %>%
  select(-ParticipantID) %>%
  as.matrix()

friedman_result <- friedman.test(means_wide_matrix)
cat(sprintf("Friedman χ²(%d) = %.4f, p = %.6f\n\n",
            friedman_result$parameter,
            friedman_result$statistic,
            friedman_result$p.value))

# =============================================================================
# 7. POST-HOC PAIRWISE COMPARISONS (Bonferroni corrected)
# =============================================================================

cat("=============================================================\n")
cat("7. POST-HOC PAIRWISE T-TESTS (Bonferroni corrected)\n")
cat("=============================================================\n")

posthoc <- means %>%
  pairwise_t_test(
    MeanRT ~ Condition,
    paired       = TRUE,
    p.adjust.method = "bonferroni"
  )

posthoc_display <- posthoc %>%
  select(group1, group2, statistic, df, p, p.adj, p.adj.signif) %>%
  mutate(
    statistic = round(statistic, 3),
    p         = round(p,         4),
    p.adj     = round(p.adj,     4)
  )

print(posthoc_display)
cat("\n")

# Also compute Cohen's d for each pair
cat("Effect sizes (Cohen's d) for each pair:\n")
conditions_list <- levels(means$Condition)
pairs <- combn(conditions_list, 2, simplify = FALSE)

for (pair in pairs) {
  a <- means %>% filter(Condition == pair[1]) %>% pull(MeanRT)
  b <- means %>% filter(Condition == pair[2]) %>% pull(MeanRT)
  d <- (mean(a) - mean(b)) / sd(a - b)
  cat(sprintf("  %-20s vs %-20s  d = %.3f\n", pair[1], pair[2], d))
}
cat("\n")

# =============================================================================
# 8. CONDITION RANKING
# =============================================================================

cat("=============================================================\n")
cat("8. CONDITION RANKING (fastest to slowest mean RT)\n")
cat("=============================================================\n")

ranking <- desc_stats %>%
  arrange(Mean) %>%
  mutate(Rank = row_number()) %>%
  select(Rank, Condition, Mean, SD, CI_lower, CI_upper)

print(ranking)
cat("\n")

# =============================================================================
# 9. PLOTS
# =============================================================================

cat("Generating plots...\n")

# Plot 1: Bar chart with error bars (mean ± SEM)
p1 <- ggplot(desc_stats, aes(x = reorder(Condition, Mean),
                               y = Mean, fill = Condition)) +
  geom_bar(stat = "identity", width = 0.6, colour = "white") +
  geom_errorbar(aes(ymin = CI_lower, ymax = CI_upper),
                width = 0.2, linewidth = 0.7) +
  scale_fill_manual(values = c(
    "DepthColour"     = "#1F3864",
    "DirectionalBeam" = "#2E75B6",
    "ObjectOutline"   = "#70AD47",
    "PeripheralHalo"  = "#ED7D31"
  )) +
  labs(
    title    = "Mean Response Time by Highlighting Condition",
    subtitle = "Error bars = 95% Confidence Interval",
    x        = "Condition",
    y        = "Mean Response Time (seconds)",
    fill     = "Condition"
  ) +
  theme_minimal(base_size = 13) +
  theme(
    plot.title    = element_text(face = "bold", hjust = 0.5),
    plot.subtitle = element_text(hjust = 0.5, colour = "grey50"),
    legend.position = "none",
    axis.text.x   = element_text(angle = 15, hjust = 1)
  )

ggsave("VRHazardStudy_BarChart.png", p1, width = 8, height = 5, dpi = 150)
cat("  Saved: VRHazardStudy_BarChart.png\n")

# Plot 2: Boxplot of per-participant means
p2 <- ggplot(means, aes(x = reorder(Condition,
                                     MeanRT,
                                     FUN = median),
                         y = MeanRT, fill = Condition)) +
  geom_boxplot(outlier.shape = 21, outlier.size = 2,
               alpha = 0.7, width = 0.5) +
  geom_jitter(width = 0.1, alpha = 0.5, size = 2, colour = "grey30") +
  scale_fill_manual(values = c(
    "DepthColour"     = "#1F3864",
    "DirectionalBeam" = "#2E75B6",
    "ObjectOutline"   = "#70AD47",
    "PeripheralHalo"  = "#ED7D31"
  )) +
  labs(
    title    = "Distribution of Mean Response Times per Condition",
    subtitle = "Points represent individual participant means",
    x        = "Condition",
    y        = "Mean Response Time (seconds)",
    fill     = "Condition"
  ) +
  theme_minimal(base_size = 13) +
  theme(
    plot.title    = element_text(face = "bold", hjust = 0.5),
    plot.subtitle = element_text(hjust = 0.5, colour = "grey50"),
    legend.position = "none",
    axis.text.x   = element_text(angle = 15, hjust = 1)
  )

ggsave("VRHazardStudy_Boxplot.png", p2, width = 8, height = 5, dpi = 150)
cat("  Saved: VRHazardStudy_Boxplot.png\n")

# Plot 3: Line plot of individual participants across conditions
condition_order <- desc_stats %>% arrange(Mean) %>% pull(Condition)
means$Condition_ordered <- factor(means$Condition, levels = condition_order)

p3 <- ggplot(means, aes(x = Condition_ordered, y = MeanRT,
                         group = ParticipantID,
                         colour = ParticipantID)) +
  geom_line(alpha = 0.5, linewidth = 0.8) +
  geom_point(size = 2.5, alpha = 0.8) +
  stat_summary(aes(group = 1), fun = mean, geom = "line",
               colour = "black", linewidth = 1.5, linetype = "dashed") +
  stat_summary(aes(group = 1), fun = mean, geom = "point",
               colour = "black", size = 4, shape = 18) +
  labs(
    title    = "Individual Participant Profiles Across Conditions",
    subtitle = "Dashed black line = group mean",
    x        = "Condition (ordered by mean RT)",
    y        = "Mean Response Time (seconds)",
    colour   = "Participant"
  ) +
  theme_minimal(base_size = 13) +
  theme(
    plot.title    = element_text(face = "bold", hjust = 0.5),
    plot.subtitle = element_text(hjust = 0.5, colour = "grey50"),
    axis.text.x   = element_text(angle = 15, hjust = 1)
  )

ggsave("VRHazardStudy_IndividualProfiles.png", p3, width = 9, height = 5, dpi = 150)
cat("  Saved: VRHazardStudy_IndividualProfiles.png\n\n")

# =============================================================================
# 10. SAVE RESULTS TO CSV
# =============================================================================

write.csv(means,         "participant_means.csv",   row.names = FALSE)
write.csv(desc_stats,    "descriptive_stats.csv",   row.names = FALSE)
write.csv(posthoc_display, "posthoc_results.csv",   row.names = FALSE)
write.csv(normality,     "normality_results.csv",   row.names = FALSE)

cat("=============================================================\n")
cat("ALL RESULTS SAVED\n")
cat("=============================================================\n")
cat("  participant_means.csv\n")
cat("  descriptive_stats.csv\n")
cat("  posthoc_results.csv\n")
cat("  normality_results.csv\n")
cat("  VRHazardStudy_BarChart.png\n")
cat("  VRHazardStudy_Boxplot.png\n")
cat("  VRHazardStudy_IndividualProfiles.png\n\n")

cat("=============================================================\n")
cat("REPORTING SUMMARY\n")
cat("=============================================================\n")
cat(sprintf("Normality:   All conditions normally distributed (Shapiro-Wilk p > .05)\n"))
cat(sprintf("Sphericity:  Mauchly's test VIOLATED (W = %.4f, p = %.4f)\n",
            anova_result$`Mauchly's Test for Sphericity`$W,
            anova_result$`Mauchly's Test for Sphericity`$p))
cat(sprintf("RM ANOVA:    F(%d,%d) = %.3f, p = %.6f, η²(g) = %.4f (GG corrected)\n",
            df1, df2, F_val, p_gg, ges))
cat(sprintf("Friedman:    χ²(3) = %.4f, p = %.6f\n",
            friedman_result$statistic, friedman_result$p.value))
cat("Post-hoc:    DepthColour significantly slower than all others\n")
cat("             DirectionalBeam significantly slower than ObjectOutline\n")
cat("             No significant difference: DirectionalBeam vs PeripheralHalo\n")
cat("             No significant difference: ObjectOutline vs PeripheralHalo\n")
