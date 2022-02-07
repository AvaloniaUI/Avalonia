const webpack = require('webpack');
const path = require('path');
const LiveReloadPlugin = require('webpack-livereload-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const CompressionPlugin = require('compression-webpack-plugin');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');
const prod = process.env.NODE_ENV == 'production';

class Printer {
    apply(compiler) {
        compiler.hooks.afterEmit.tap("Printer", ()=> console.log("Build completed at " + new Date().toString()));
        compiler.hooks.watchRun.tap("Printer", ()=> console.log("Watch triggered at " + new Date().toString()));
    }
}

const config = {
    stats: {
        errorDetails: true
    },
    entry: {
            bundle: './src/index.tsx'
        },
    output: {
        path: path.resolve(__dirname, 'build'),
        publicPath: '/',
        filename: '[name].[chunkhash].js'
    },
    performance: { hints: false },
    mode: prod ? "production" : "development",
    module: {
        rules: [
            {
                enforce: "pre",
                test: /\.js$/,
                loader: "source-map-loader",
                exclude: [
                    path.resolve(__dirname, 'node_modules/mobx-state-router')
                ]
            },
            {
                "oneOf": [
                    {
                        test: /\.(ts|tsx)$/,
                        exclude: /node_modules/,
                        use: 'ts-loader'
                    },
                    {
                        test: /\.css$/,
                        use: [
                            MiniCssExtractPlugin.loader,
                            'css-loader'
                        ]
                    },
                    {
                        test: /\.(jpg|png)$/,
                        use: {
                            loader: "url-loader",
                            options: {
                                limit: 25000,
                            },
                        },
                    },
                    {
                        test: /.(ttf|otf|eot|svg|woff(2)?)(\?[a-z0-9]+)?$/,
                        use: [{
                            loader: 'file-loader',
                            options: {
                                name: '[name].[ext]',
                                outputPath: 'fonts/',    // where the fonts will go
                            }
                        }]
                    },
                    {
                        loader: require.resolve('file-loader'),
                        exclude: [/\.(js|jsx|mjs|tsx|ts)$/, /\.html$/, /\.json$/],
                        options: {
                            name: 'assets/[name].[hash:8].[ext]',
                        },
                    }]
            },

        ]
    },
    devtool: "source-map",
    resolve: {
        modules: [path.resolve(__dirname, 'node_modules')],
        plugins: [new TsconfigPathsPlugin({ configFile: "./tsconfig.json", logLevel: 'info' })],
        extensions: ['.ts', '.tsx', '.js', '.json'],
        alias: {
            'src': path.resolve(__dirname, 'src')
        }
    },
    plugins:
        [
        new Printer(),
        new CleanWebpackPlugin({
            cleanAfterEveryBuildPatterns: ['dist']
        }),
        new MiniCssExtractPlugin({
            filename: "[name].[chunkhash]h" +
            ".css",
            chunkFilename: "[id].[chunkhash].css"
        }),
        new LiveReloadPlugin({appendScriptTag: !prod}),
        new HtmlWebpackPlugin({
            template: path.resolve(__dirname, './src/index.html'),
            filename: 'index.html' //relative to root of the application
        }),
        new CompressionPlugin({
            test: /(\?.*)?$/i
        })
    ]
};
module.exports = config;
