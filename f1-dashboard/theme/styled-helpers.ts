import type { AppUiTheme } from "@/theme/ui-theme";

/** Cast styled-components unknown theme to typed AppUiTheme */
export const t = (theme: unknown) => theme as AppUiTheme;
