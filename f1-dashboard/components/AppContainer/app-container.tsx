import { AppContainerStyled } from "./app-container.styled";

export function AppContainer({ children }: { children: React.ReactNode }) {
  return <AppContainerStyled>{children}</AppContainerStyled>;
}
