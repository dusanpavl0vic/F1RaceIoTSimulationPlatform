type F1LogoIconProps = {
  size?: number;
  fillColor?: string;
  strokeColor?: string;
};

export function F1LogoIcon({
  size = 36,
  fillColor = "#002455",
  strokeColor = "#FF3838",
}: F1LogoIconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 300 300"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      <path
        d="M278.125 114.412H241.112L169.931 185.588H206.944L278.125 114.412Z"
        fill={fillColor}
        stroke={strokeColor}
        strokeWidth="6.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M241.112 114.412H111.175C105.43 114.412 99.7402 115.543 94.4319 117.742C89.1237 119.94 84.3004 123.163 80.2375 127.225L21.875 185.588H58.8875L99.9375 144.544C101.969 142.513 104.381 140.901 107.035 139.802C109.689 138.703 112.534 138.137 115.406 138.137H217.388"
        fill={fillColor}
        stroke={strokeColor}
        strokeWidth="6.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M58.8875 185.588H95.9L116.881 164.612C117.752 163.741 118.785 163.049 119.923 162.578C121.061 162.106 122.281 161.863 123.512 161.863H193.656"
        stroke={strokeColor}
        strokeWidth="6.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <line
        x1="239.71"
        y1="114.71"
        x2="214.71"
        y2="139.71"
        stroke={strokeColor}
        strokeWidth="6.25"
      />
    </svg>
  );
}
