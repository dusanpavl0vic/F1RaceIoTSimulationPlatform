import {
  StyledAppFooter,
  StyledAppFooterInner,
  StyledAppFooterLink,
} from "@/components/layout/footer/app-footer.styles";

export function AppFooter() {
  return (
    <StyledAppFooter $borderColor="border">
      <StyledAppFooterInner>
        <div>
          <strong>Dusan Pavlovic 18820</strong>
          <p>Elektronski fakultet, Univerzitet u Nisu</p>
        </div>

        <StyledAppFooterLink
          $backgroundColor="formulaRed"
          $textColor="white"
          href="https://github.com/dusanpavl0vic"
          target="_blank"
          rel="noreferrer"
        >
          GitHub
        </StyledAppFooterLink>
      </StyledAppFooterInner>
    </StyledAppFooter>
  );
}
