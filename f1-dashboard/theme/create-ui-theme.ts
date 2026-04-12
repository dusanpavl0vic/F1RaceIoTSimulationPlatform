import type { ColorMode } from "@/features/app/store/app-ui-slice";
import { appColors } from "@/theme/colors";
import { appBreakpoints, appDevices, type AppUiTheme } from "@/theme/ui-theme";

export function createAppUiTheme(colorMode: ColorMode): AppUiTheme {
  const isDark = colorMode === "dark";

  return {
    isDark,
    colors: {
      ...appColors,
      background: isDark ? appColors.backgroundDark : appColors.background,
      backgroundSoft: isDark ? appColors.backgroundSoftDark : appColors.backgroundSoft,
      panel: isDark ? appColors.panelDark : appColors.panel,
      textPrimary: isDark ? appColors.textPrimaryDark : appColors.textPrimary,
      textMuted: isDark ? appColors.textMutedDark : appColors.textMuted,
      border: isDark ? appColors.borderDark : appColors.border,
      shadow: isDark ? appColors.shadowDark : appColors.shadow,
    },
    breakpoints: appBreakpoints,
    devices: appDevices,
    layout: {
      contentWidth: "min(1240px, calc(100vw - 32px))",
      mobileContentWidth: "min(100vw - 20px, 100%)",
    },
    radius: {
      panel: "4px",
      card: "4px",
      pill: "4px",
    },
  };
}
