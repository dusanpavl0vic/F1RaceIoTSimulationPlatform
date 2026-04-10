import "styled-components";
import type { AppUiTheme } from "@/theme/ui-theme";

declare module "styled-components" {
  export interface DefaultTheme extends AppUiTheme {}
}
