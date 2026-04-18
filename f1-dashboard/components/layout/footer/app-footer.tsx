import {
  StyledFooter,
  StyledFooterIdentity,
  StyledFooterInner,
  StyledFooterName,
  StyledFooterUniversity,
  StyledGithubLink,
} from "./app-footer.styles";

export function AppFooter() {
  return (
    <StyledFooter>
      <StyledFooterInner>
        <StyledFooterIdentity>
          <StyledFooterName>DUSAN PAVLOVIC 18820</StyledFooterName>
          <StyledFooterUniversity>
            ELEKTRONSKI FAKULTET · UNIVERZITET U NISU
          </StyledFooterUniversity>
        </StyledFooterIdentity>

        <StyledGithubLink
          href="https://github.com/dusanpavl0vic"
          rel="noreferrer"
        >
          GITHUB
        </StyledGithubLink>
      </StyledFooterInner>
    </StyledFooter>
  );
}
